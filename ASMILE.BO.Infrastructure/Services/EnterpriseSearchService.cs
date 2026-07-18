using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class EnterpriseSearchService : IEnterpriseSearchService
{
    private readonly AppDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrentCompanyContext _companyContext;
    private readonly IAuthorizationService? _authorizationService;

    public EnterpriseSearchService(AppDbContext dbContext, ICurrentUserService currentUserService, ICurrentCompanyContext? companyContext = null, IAuthorizationService? authorizationService = null)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _companyContext = companyContext ?? new NullCompanyContext();
        _authorizationService = authorizationService;
    }

    public async Task UpsertAsync(SearchIndexUpsertRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var entityType = NormalizeRequired(request.EntityType, nameof(request.EntityType));
        var entityId = NormalizeRequired(request.EntityId, nameof(request.EntityId));
        var title = NormalizeRequired(request.Title, nameof(request.Title));
        var searchText = NormalizeSearchText(title, request.Subtitle, request.Category, request.SearchText, entityType, entityId);

        var entry = await _dbContext.SearchIndexEntries
            .FirstOrDefaultAsync(x => x.EntityType == entityType && x.EntityId == entityId, cancellationToken);

        if (entry is null)
        {
            entry = new SearchIndexEntry { EntityType = entityType, EntityId = entityId, IndexedAt = DateTime.UtcNow };
            _dbContext.SearchIndexEntries.Add(entry);
        }

        entry.Title = title;
        entry.Subtitle = request.Subtitle?.Trim() ?? string.Empty;
        entry.Category = string.IsNullOrWhiteSpace(request.Category) ? entityType : request.Category.Trim();
        entry.TargetType = string.IsNullOrWhiteSpace(request.TargetType) ? "module" : request.TargetType.Trim();
        entry.TargetRoute = request.TargetRoute?.Trim() ?? string.Empty;
        entry.TargetGuid = request.TargetGuid;
        entry.SourceType = request.SourceType?.Trim();
        entry.MetadataJson = request.MetadataJson?.Trim();
        entry.SearchText = searchText;
        entry.BaseScore = Math.Clamp(request.BaseScore, 0, 1000);
        entry.IsActive = true;
        entry.IsArchived = false;
        entry.ArchivedAtUtc = null;
        entry.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchIndexEntryDto>> SearchAsync(string query, int take = 30, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var term = query.Trim();
        var normalized = term.ToUpperInvariant();
        var recentBoost = await GetRecentSearchBoostAsync(normalized, cancellationToken);
        var canViewOperationalLogs = _authorizationService?.HasPermission("Logs.View") != false;
        await _companyContext.EnsureInitializedAsync(cancellationToken);
        var rows = await _dbContext.SearchIndexEntries.AsNoTracking()
            .ApplyCompanyBranchScope(_companyContext.CompanyId, _companyContext.BranchId)
            .Where(x => x.IsActive && (includeArchived || !x.IsArchived))
            .Take(Math.Clamp(take * 20, 80, 400))
            .ToListAsync(cancellationToken);

        return rows
            .Where(x => CanViewEntry(x, canViewOperationalLogs))
            .Where(x => MatchesSearch(x, term, normalized))
            .Select(x => ToDto(x, Score(x, normalized, recentBoost)))
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.SourceType == "workflow" ? 1 : 0)
            .ThenBy(x => x.Title)
            .Take(Math.Clamp(take, 1, 100))
            .ToArray();
    }

    public async Task RecordRecentSearchAsync(string query, SearchIndexEntryDto? selected = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(query))
        {
            return;
        }

        await _companyContext.EnsureInitializedAsync(cancellationToken);
        _dbContext.RecentSearches.Add(new RecentSearch
        {
            UserId = _currentUserService.CurrentUser?.Id,
            Query = query.Trim(),
            SelectedEntityType = selected?.EntityType,
            SelectedEntityId = selected?.EntityId,
            MetadataJson = selected?.MetadataJson,
            IsArchived = false,
            SearchedAt = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecentSearchDto>> GetRecentSearchesAsync(int take = 10, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var userId = _currentUserService.CurrentUser?.Id;
        await _companyContext.EnsureInitializedAsync(cancellationToken);
        return await _dbContext.RecentSearches.AsNoTracking()
            .ApplyCompanyBranchScope(_companyContext.CompanyId, _companyContext.BranchId)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.SearchedAt)
            .Take(Math.Clamp(take, 1, 50))
            .Select(x => new RecentSearchDto { Id = x.Id, Query = x.Query, SearchedAt = x.SearchedAt, MetadataJson = x.MetadataJson })
            .ToListAsync(cancellationToken);
    }

    private static SearchIndexEntryDto ToDto(SearchIndexEntry entry, int score) => new()
    {
        Id = entry.Id,
        EntityType = entry.EntityType,
        EntityId = entry.EntityId,
        Title = entry.Title,
        Subtitle = entry.Subtitle,
        Category = entry.Category,
        TargetType = entry.TargetType,
        TargetRoute = entry.TargetRoute,
        TargetGuid = entry.TargetGuid,
        SourceType = entry.SourceType,
        MetadataJson = entry.MetadataJson,
        Score = score
    };

    private static int Score(SearchIndexEntry entry, string normalizedTerm, int recentBoost)
    {
        var score = entry.BaseScore;
        var title = entry.Title.ToUpperInvariant();
        var subtitle = entry.Subtitle.ToUpperInvariant();
        var searchText = entry.SearchText.ToUpperInvariant();
        var category = entry.Category.ToUpperInvariant();
        var metadata = entry.MetadataJson?.ToUpperInvariant();

        if (title == normalizedTerm) score += 1000;
        if (title.StartsWith(normalizedTerm, StringComparison.OrdinalIgnoreCase)) score += 500;
        if (subtitle.StartsWith(normalizedTerm, StringComparison.OrdinalIgnoreCase)) score += 250;
        if (searchText.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase)) score += 120;
        if (category.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase)) score += 80;
        if (!string.IsNullOrWhiteSpace(metadata) && metadata.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase)) score += 60;

        var tokens = normalizedTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var token in tokens)
        {
            score += TokenScore(title, token) * 40;
            score += TokenScore(subtitle, token) * 24;
            score += TokenScore(searchText, token) * 12;
            if (!string.IsNullOrWhiteSpace(metadata))
            {
                score += TokenScore(metadata, token) * 16;
            }
        }

        score += recentBoost;
        score += SourceBoost(entry.SourceType);
        return score;
    }

    private async Task<int> GetRecentSearchBoostAsync(string normalizedQuery, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.CurrentUser?.Id;
        if (userId is null)
        {
            return 0;
        }

        var recent = await _dbContext.RecentSearches.AsNoTracking()
            .ApplyCompanyBranchScope(_companyContext.CompanyId, _companyContext.BranchId)
            .Where(x => x.UserId == userId.Value && !x.IsArchived)
            .OrderByDescending(x => x.SearchedAt)
            .Take(20)
            .Select(x => x.Query)
            .ToListAsync(cancellationToken);

        var boost = 0;
        foreach (var query in recent)
        {
            var normalizedRecent = query.ToUpperInvariant();
            if (normalizedRecent == normalizedQuery)
            {
                boost += 120;
            }
            else if (normalizedRecent.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) || normalizedQuery.Contains(normalizedRecent, StringComparison.OrdinalIgnoreCase))
            {
                boost += 60;
            }
        }

        return boost;
    }

    private static bool CanViewEntry(SearchIndexEntry entry, bool canViewOperationalLogs)
    {
        if (canViewOperationalLogs)
        {
            return true;
        }

        return !string.Equals(entry.SourceType, "audit", StringComparison.OrdinalIgnoreCase)
               && !string.Equals(entry.SourceType, "telemetry", StringComparison.OrdinalIgnoreCase)
               && !string.Equals(entry.SourceType, "workflow", StringComparison.OrdinalIgnoreCase)
               && !entry.EntityType.Contains("Audit", StringComparison.OrdinalIgnoreCase)
               && !entry.EntityType.Contains("Telemetry", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesSearch(SearchIndexEntry entry, string term, string normalizedTerm)
    {
        if (entry.SearchText.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase)
            || entry.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
            || entry.Subtitle.Contains(term, StringComparison.OrdinalIgnoreCase)
            || entry.Category.Contains(term, StringComparison.OrdinalIgnoreCase)
            || (entry.MetadataJson != null && entry.MetadataJson.Contains(term, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var tokens = normalizedTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return false;
        }

        var searchable = string.Join(' ', entry.Title, entry.Subtitle, entry.Category, entry.SearchText, entry.MetadataJson ?? string.Empty);
        return tokens.Any(token => TokenScore(searchable, token) > 0);
    }

    private static int SourceBoost(string? sourceType)
        => sourceType?.ToLowerInvariant() switch
        {
            "workflow" => 70,
            "audit" => 60,
            "telemetry" => 50,
            "report" => 30,
            "import" => 30,
            _ => 0
        };

    private static int TokenScore(string haystack, string token)
    {
        if (string.IsNullOrWhiteSpace(haystack) || string.IsNullOrWhiteSpace(token))
        {
            return 0;
        }

        if (haystack.Contains(token, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        var best = haystack.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Max(part => Similarity(part, token));
        return best >= 0.8d ? 2 : best >= 0.6d ? 1 : 0;
    }

    private static double Similarity(string left, string right)
    {
        var distance = Levenshtein(left.ToUpperInvariant(), right.ToUpperInvariant());
        var longest = Math.Max(left.Length, right.Length);
        return longest == 0 ? 1d : 1d - (double)distance / longest;
    }

    private static int Levenshtein(string source, string target)
    {
        if (source.Length == 0) return target.Length;
        if (target.Length == 0) return source.Length;

        var rows = source.Length + 1;
        var cols = target.Length + 1;
        var matrix = new int[rows, cols];
        for (var i = 0; i < rows; i++) matrix[i, 0] = i;
        for (var j = 0; j < cols; j++) matrix[0, j] = j;

        for (var i = 1; i < rows; i++)
        {
            for (var j = 1; j < cols; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source.Length, target.Length];
    }

    private static string NormalizeSearchText(params string?[] values)
        => string.Join(' ', values.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim())).ToUpperInvariant();

    private static string NormalizeRequired(string? value, string name)
        => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.", name) : value.Trim();

    private sealed class NullCompanyContext : ICurrentCompanyContext
    {
        public Guid? CompanyId => null;
        public Guid? BranchId => null;
        public string CompanyName => "Default Company";
        public string? BranchName => null;
        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>([]);
        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BranchContextOptionDto>>([]);
    }
}
