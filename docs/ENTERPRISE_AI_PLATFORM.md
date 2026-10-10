# ASMILE ERP — ENTERPRISE AI PLATFORM

## Kiến trúc AI cho toàn bộ hệ sinh thái ASMILE

**Phiên bản:** 1.0
**Ngày:** 2026-07-20
**Loại:** Enterprise AI Platform Architecture
**Mục tiêu chất lượng:** Microsoft Copilot · Azure AI Foundry · OpenAI Enterprise · Google Vertex AI · Semantic Kernel · LangGraph

---

# PHẦN 1: AI PLATFORM OVERVIEW

## 1.1. AI Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   AI CONSUMPTION LAYER                       │
│  AI Assistant (WPF) │ AI Chat │ AI Dashboard │ AI Copilot   │
│  AI Automation │ AI Recommendations │ AI Alerts             │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   AI AGENT LAYER                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐  │
│  │Front Offc│ │Housekeep │ │ NightAud │ │  Concierge   │  │
│  │  Agent   │ │  Agent   │ │  Agent   │ │    Agent     │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐  │
│  │Restaurant│ │ Banquet  │ │   CRM    │ │  Accounting  │  │
│  │  Agent   │ │  Agent   │ │  Agent   │ │    Agent     │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   AI ORCHESTRATOR (KERNEL) ← THIS DOCUMENT  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐  │
│  │ Prompt   │ │ Context  │ │ Memory   │ │  Reasoning   │  │
│  │ Engine   │ │ Engine   │ │ Engine   │ │   Engine     │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐  │
│  │   RAG    │ │  Model   │ │   Tool   │ │  Workflow    │  │
│  │ Pipeline │ │  Router  │ │  Calling │ │   Engine     │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐  │
│  │ Guardrail│ │  Audit   │ │Monitoring│ │  Evaluation  │  │
│  │  System  │ │  System  │ │  System  │ │   System     │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────┼───────────────────────────────┐
│                   AI INFRASTRUCTURE LAYER                    │
│  LLM Gateway │ Vector DB │ Embedding Service │ Cache        │
│  OpenAI │ Claude │ Gemini │ DeepSeek │ Local LLM            │
└─────────────────────────────────────────────────────────────┘
```

## 1.2. Core Design Principles

```yaml
PRINCIPLE 1: AI ASSISTS, NEVER CONTROLS
  AI đề xuất. Con người quyết định.
  AI không bao giờ tự động thay đổi giá phòng, trạng thái đặt phòng,
  hoặc thực hiện giao dịch tài chính mà không có xác nhận của con người.

PRINCIPLE 2: MODEL AGNOSTIC
  Business logic không biết đang dùng OpenAI, Claude, hay Gemini.
  Thay đổi model không cần sửa code nghiệp vụ.
  Model Router tự chọn model tối ưu cho từng tác vụ.

PRINCIPLE 3: CONTEXT IS EVERYTHING
  AI response quality = function(context quality).
  Context Engine tự động thu thập mọi thông tin liên quan
  trước khi gửi prompt đến LLM.

PRINCIPLE 4: OBSERVABLE AND AUDITABLE
  Mọi prompt, response, tool call, và decision đều được ghi log.
  Có thể trace toàn bộ chain-of-thought.
  AI Audit log giúp giải thích TẠI SAO AI đề xuất điều gì đó.

PRINCIPLE 5: OFFLINE-FIRST FOR CRITICAL PATHS
  Các tác vụ quan trọng (check-in, payment) phải hoạt động
  ngay cả khi LLM không khả dụng.
  AI là trợ lý, không phải dependency.
```

---

# PHẦN 2: AI ORCHESTRATOR — THE KERNEL

## 2.1. Orchestrator Interface

```csharp
/// <summary>
/// AI Orchestrator is the SINGLE entry point for ALL AI operations.
/// Every agent, every workflow, every tool call goes through here.
/// This ensures consistent: logging, audit, cost tracking, rate limiting.
/// </summary>
public interface IAIOrchestrator
{
    // ===== CORE =====
    Task<AIResponse> ExecuteAsync(AIRequest request, CancellationToken ct = default);
    IAsyncEnumerable<AIStreamChunk> ExecuteStreamingAsync(AIRequest request, CancellationToken ct = default);

    // ===== AGENT MANAGEMENT =====
    void RegisterAgent(IAIAgent agent);
    IAIAgent GetAgent(string agentId);
    IReadOnlyList<IAIAgent> GetAllAgents();
}

public class AIRequest
{
    public string AgentId { get; set; }              // "front-office-agent"
    public string Intent { get; set; }               // "check_availability"
    public string UserMessage { get; set; }          // Raw user input
    public string ConversationId { get; set; }       // For multi-turn
    public Dictionary<string, object> Context { get; set; }
    public AIRequestOptions Options { get; set; }
    public string UserId { get; set; }
    public string[] RequiredPermissions { get; set; }
}

public class AIRequestOptions
{
    public string PreferredModel { get; set; }       // null = auto-select
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 2000;
    public bool EnableTools { get; set; } = true;
    public bool EnableRAG { get; set; } = true;
    public bool EnableStreaming { get; set; } = false;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

public class AIResponse
{
    public string ResponseId { get; set; }
    public string Content { get; set; }
    public string ModelUsed { get; set; }
    public int TokensUsed { get; set; }
    public int TokensPrompt { get; set; }
    public int TokensCompletion { get; set; }
    public decimal CostIncurred { get; set; }
    public TimeSpan Latency { get; set; }
    public List<ToolCall> ToolCalls { get; set; }
    public List<Citation> Citations { get; set; }
    public List<string> Sources { get; set; }
    public float Confidence { get; set; }
    public string ReasoningTrace { get; set; }       // Chain-of-thought
    public DateTime CompletedAtUtc { get; set; }
}
```

## 2.2. Orchestrator Pipeline

```
AIRequest enters Orchestrator
  │
  ├── 1. Guardrail Check (INPUT)
  │   ├── Prompt injection detection
  │   ├── Content moderation
  │   ├── Permission check
  │   └── Rate limit check
  │
  ├── 2. Context Assembly
  │   ├── Business Context (hotel, property, shift, business date)
  │   ├── User Context (role, permissions, preferences, history)
  │   ├── Conversation Context (previous turns, memory)
  │   └── Domain Context (relevant entities: guest, room, reservation)
  │
  ├── 3. RAG Retrieval (if enabled)
  │   ├── Embed query
  │   ├── Search Vector DB
  │   ├── Search Knowledge Base (policies, SOPs, FAQ)
  │   └── Rank & filter results
  │
  ├── 4. Prompt Assembly
  │   ├── Load Prompt Template from Registry
  │   ├── Inject variables (context, RAG results, user input)
  │   ├── Inject system prompt (agent personality, rules)
  │   └── Inject tool definitions
  │
  ├── 5. Model Routing
  │   ├── Select optimal model (cost, latency, capability)
  │   ├── Apply rate limiting per model
  │   └── Fallback if primary model unavailable
  │
  ├── 6. LLM Execution
  │   ├── Send prompt to LLM Gateway
  │   ├── Stream response (if streaming)
  │   └── Handle tool calls (Function Calling)
  │
  ├── 7. Tool Execution (if tools called)
  │   ├── Validate tool call (parameters, permissions)
  │   ├── Execute tool (call business service)
  │   ├── Return result to LLM for final response
  │   └── Loop: LLM may call more tools
  │
  ├── 8. Guardrail Check (OUTPUT)
  │   ├── Hallucination detection
  │   ├── Content moderation
  │   ├── PII leak detection
  │   └── Business rule validation
  │
  ├── 9. Response Assembly
  │   ├── Format response for UI
  │   ├── Attach citations & sources
  │   ├── Attach confidence score
  │   └── Attach reasoning trace
  │
  └── 10. Audit & Telemetry
      ├── Log full interaction
      ├── Update cost tracking
      ├── Update metrics
      └── Store for evaluation
```

---

# PHẦN 3: PROMPT ENGINE

## 3.1. Prompt Registry

```csharp
public interface IPromptRegistry
{
    PromptTemplate GetTemplate(string templateCode, int? version = null);
    PromptTemplate GetLatestTemplate(string templateCode);
    void RegisterTemplate(PromptTemplate template);
    Task<PromptTemplate> CreateNewVersionAsync(string templateCode, string newContent);

    // Testing
    Task<PromptTestResult> TestTemplateAsync(string templateCode,
        Dictionary<string, string> testVariables, string modelId);
}

public class PromptTemplate
{
    public string TemplateCode { get; set; }         // "FO_CHECK_AVAILABILITY"
    public int Version { get; set; }
    public string SystemPrompt { get; set; }
    public string UserPromptTemplate { get; set; }   // With {{variable}} placeholders
    public string[] RequiredVariables { get; set; }
    public string[] RequiredTools { get; set; }
    public string ModelPreference { get; set; }       // "claude-sonnet-5" or null
    public PromptStatus Status { get; set; }           // Draft, Testing, Approved, Active, Deprecated
    public string ApprovedBy { get; set; }
    public DateTime ApprovedAtUtc { get; set; }
    public List<PromptTestResult> TestResults { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

public enum PromptStatus { Draft, Testing, Approved, Active, Deprecated }
```

## 3.2. Prompt Template Catalog (Front Office Examples)

```yaml
FO_CHECK_AVAILABILITY:
  System: "Bạn là trợ lý lễ tân khách sạn ASMILE. Nhiệm vụ của bạn là kiểm tra
           tình trạng phòng trống và đề xuất phương án cho khách.
           LUÔN trả lời bằng tiếng Việt.
           LUÔN kiểm tra dữ liệu thực tế qua tool, không suy đoán.
           KHÔNG đưa ra cam kết về giá nếu chưa kiểm tra Rate Engine."
  Variables: [guest_name, arrival_date, departure_date, adults, children, preferences]
  Tools: [check_room_availability, calculate_price, search_guest_profile]

FO_CHECK_IN_GUEST:
  System: "Bạn là trợ lý check-in khách sạn. Hướng dẫn nhân viên lễ tân
           thực hiện quy trình check-in chuyên nghiệp.
           Nhắc nhở các bước quan trọng: kiểm tra giấy tờ, xác nhận đặt cọc,
           chọn phòng phù hợp, in phiếu đăng ký, phát thẻ phòng."
  Variables: [reservation_number, guest_name, room_number]
  Tools: [get_reservation, check_in_stay, create_folio]

FO_NIGHT_AUDIT_REVIEW:
  System: "Bạn là trợ lý Night Audit. Phân tích báo cáo Night Audit
           và đưa ra các điểm bất thường cần chú ý.
           So sánh với các ngày trước đó. Đề xuất hành động khắc phục."
  Variables: [business_date]
  Tools: [get_night_audit_report, compare_previous_audits, get_outstanding_balance]

HK_ROOM_PRIORITY:
  System: "Bạn là trợ lý buồng phòng. Dựa trên danh sách phòng bẩn,
           khách sắp đến, và nhân viên đang làm việc, đề xuất thứ tự ưu tiên
           dọn phòng tối ưu."
  Variables: [business_date]
  Tools: [get_dirty_rooms, get_expected_arrivals, get_housekeeping_staff]

RV_RATE_RECOMMENDATION:
  System: "Bạn là trợ lý Revenue Management. Phân tích occupancy forecast,
           competitor rates (nếu có), và historical data để đề xuất
           điều chỉnh giá cho 30 ngày tới. Giải thích lý do cho từng đề xuất."
  Variables: [property_id]
  Tools: [get_occupancy_forecast, get_historical_rates, get_current_rates]
```

---

# PHẦN 4: CONTEXT ENGINE

## 4.1. Context Assembly

```csharp
public interface IContextEngine
{
    Task<AIContext> AssembleContextAsync(AIRequest request, CancellationToken ct);
}

public class AIContext
{
    // Business Context
    public BusinessContext Business { get; set; }

    // User Context
    public UserContext User { get; set; }

    // Conversation Context
    public ConversationContext Conversation { get; set; }

    // Domain Context
    public DomainContext Domain { get; set; }

    // Serialized for prompt injection
    public string ToPromptContext()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== BUSINESS CONTEXT ===");
        sb.AppendLine($"Hotel: {Business.HotelName}");
        sb.AppendLine($"Property: {Business.PropertyName}");
        sb.AppendLine($"Business Date: {Business.BusinessDate:dd/MM/yyyy}");
        sb.AppendLine($"Current Shift: {Business.Shift}");

        sb.AppendLine("=== USER CONTEXT ===");
        sb.AppendLine($"User: {User.DisplayName}");
        sb.AppendLine($"Role: {User.Role}");
        sb.AppendLine($"Department: {User.Department}");

        sb.AppendLine("=== DOMAIN CONTEXT ===");
        if (Domain.CurrentGuest != null)
            sb.AppendLine($"Guest: {Domain.CurrentGuest.Name} " +
                $"(VIP: {Domain.CurrentGuest.VipLevel}, " +
                $"Stays: {Domain.CurrentGuest.TotalStays})");
        if (Domain.CurrentStay != null)
            sb.AppendLine($"Stay: Room {Domain.CurrentStay.RoomNumber}, " +
                $"Arrival: {Domain.CurrentStay.ArrivalDate:dd/MM}, " +
                $"Departure: {Domain.CurrentStay.DepartureDate:dd/MM}");
        if (Domain.CurrentReservation != null)
            sb.AppendLine($"Reservation: {Domain.CurrentReservation.ReservationNumber}");

        return sb.ToString();
    }
}

public class BusinessContext
{
    public string HotelName { get; set; }
    public string PropertyName { get; set; }
    public DateOnly BusinessDate { get; set; }
    public string Shift { get; set; }
    public int TotalRooms { get; set; }
    public int OccupiedRooms { get; set; }
    public decimal OccupancyPct { get; set; }
    public decimal TodayRevenue { get; set; }
}

public class UserContext
{
    public string UserId { get; set; }
    public string DisplayName { get; set; }
    public string Role { get; set; }
    public string Department { get; set; }
    public string[] Permissions { get; set; }
    public string Language { get; set; } = "vi";
}
```

---

# PHẦN 5: MEMORY ENGINE

## 5.1. Memory Architecture

```csharp
public interface IMemoryEngine
{
    // Short-term memory (conversation turns)
    Task AddToConversationAsync(string conversationId, MemoryEntry entry);
    Task<List<MemoryEntry>> GetConversationHistoryAsync(string conversationId, int maxTurns = 10);

    // Long-term memory (facts, preferences — stored in Vector DB)
    Task StoreFactAsync(string key, string value, Dictionary<string, string> metadata);
    Task<List<MemoryEntry>> RecallFactsAsync(string query, int topK = 5);

    // Session memory (current user session)
    void SetSessionValue(string key, object value);
    T GetSessionValue<T>(string key);

    // Clear
    Task ClearConversationAsync(string conversationId);
}

public class MemoryEntry
{
    public string Role { get; set; }              // "user", "assistant", "system", "tool"
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

// Memory is organized in tiers:
// L1: Session Memory (in-memory, per session, lost on close)
// L2: Conversation Memory (DB, per conversation, persisted)
// L3: Semantic Memory (Vector DB, long-term facts about guests, rooms, etc.)
```

---

# PHẦN 6: RAG PIPELINE

## 6.1. RAG Architecture

```
User Query: "Chính sách hủy phòng là gì?"
  │
  ▼
1. Embedding: Convert query to vector (768 dimensions)
  │
  ▼
2. Retrieval: Search Vector DB for top-K relevant chunks
  │  ├── Knowledge Base: policy documents, SOP, FAQ
  │  ├── Guest History: past interactions, preferences
  │  └── Business Data: rates, availability
  │
  ▼
3. Ranking: Re-rank by relevance + recency + authority
  │
  ▼
4. Context Injection: Insert top chunks into prompt
  │
  ▼
5. LLM Generation: Generate response with citations
  │
  ▼
6. Citation: Attach source references to response
```

## 6.2. RAG Interface

```csharp
public interface IRAGPipeline
{
    Task<RAGResult> RetrieveAsync(string query, RAGOptions options = null);
}

public class RAGOptions
{
    public int TopK { get; set; } = 5;
    public float MinRelevanceScore { get; set; } = 0.7f;
    public string[] KnowledgeBaseFilters { get; set; }  // ["policies", "sop"]
    public bool IncludeGuestHistory { get; set; }
    public bool IncludeBusinessData { get; set; }
    public string GuestId { get; set; }
}

public class RAGResult
{
    public List<RAGChunk> Chunks { get; set; }
    public string PromptContext { get; set; }    // Formatted for prompt injection
}

public class RAGChunk
{
    public string Content { get; set; }
    public string Source { get; set; }           // Document name or URL
    public string SourceType { get; set; }        // "Policy", "SOP", "FAQ", "GuestHistory"
    public float RelevanceScore { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}
```

## 6.3. Knowledge Base Sources

```yaml
Policies:
  - Cancellation policy
  - Check-in/Check-out policy
  - Pet policy
  - Smoking policy
  - Extra bed policy
  - Payment policy
  - Refund policy

SOP (Standard Operating Procedures):
  - Check-in procedure
  - Check-out procedure
  - Night Audit procedure
  - Cashier shift open/close
  - Housekeeping room cleaning
  - Lost & Found handling
  - Emergency procedures
  - Fire evacuation
  - Medical emergency
  - VIP guest handling

FAQ:
  - Common guest questions
  - Wi-Fi password
  - Breakfast hours
  - Pool hours
  - Gym hours
  - Airport transfer
  - Local attractions
  - Restaurant recommendations

Business Rules:
  - Rate rules
  - Discount rules
  - Upgrade rules
  - Commission rules
  - Overbooking rules
```

---

# PHẦN 7: MODEL ROUTER

## 7.1. LLM Gateway

```csharp
public interface ILLMGateway
{
    Task<LLMResponse> SendAsync(LLMRequest request, CancellationToken ct = default);
    IAsyncEnumerable<LLMStreamChunk> SendStreamingAsync(LLMRequest request, CancellationToken ct = default);
    IReadOnlyList<LLMProvider> GetAvailableProviders();
    Task<bool> IsProviderHealthyAsync(string providerId);
}

public class LLMRequest
{
    public string ModelId { get; set; }          // null = auto-select
    public List<ChatMessage> Messages { get; set; }
    public List<ToolDefinition> Tools { get; set; }
    public float Temperature { get; set; }
    public int MaxTokens { get; set; }
}

public class LLMResponse
{
    public string Content { get; set; }
    public string ModelUsed { get; set; }
    public string ProviderUsed { get; set; }
    public int TokensPrompt { get; set; }
    public int TokensCompletion { get; set; }
    public decimal Cost { get; set; }
    public TimeSpan Latency { get; set; }
    public List<ToolCall> ToolCalls { get; set; }
    public string FinishReason { get; set; }
}
```

## 7.2. Model Router Strategy

```csharp
public interface IModelRouter
{
    Task<LLMProvider> SelectProviderAsync(AIRequest request);
}

public class ModelRouter : IModelRouter
{
    public async Task<LLMProvider> SelectProviderAsync(AIRequest request)
    {
        // STRATEGY 1: User preference
        if (request.Options.PreferredModel != null)
            return GetProviderByModel(request.Options.PreferredModel);

        // STRATEGY 2: Intent-based routing
        var agent = _orchestrator.GetAgent(request.AgentId);
        var intentModel = agent.DefaultModel;
        if (intentModel != null)
            return GetProviderByModel(intentModel);

        // STRATEGY 3: Cost-optimized routing
        // Simple tasks → cheap model, complex tasks → powerful model
        if (IsSimpleIntent(request.Intent))
            return await GetCheapestAvailableProviderAsync();

        if (IsComplexReasoningIntent(request.Intent))
            return await GetMostCapableProviderAsync();

        // STRATEGY 4: Latency-optimized routing (for streaming)
        if (request.Options.EnableStreaming)
            return await GetFastestProviderAsync();

        // STRATEGY 5: Fallback chain
        return await GetDefaultProviderAsync();
    }

    private bool IsSimpleIntent(string intent) =>
        intent is "greeting" or "faq" or "translate" or "summarize";

    private bool IsComplexReasoningIntent(string intent) =>
        intent is "night_audit_review" or "rate_recommendation"
            or "forecast" or "anomaly_detection";
}
```

## 7.3. Provider Registry

```yaml
providers:
  - id: "openai"
    models:
      - "gpt-4o"          # Complex reasoning, $5/1M input, $15/1M output
      - "gpt-4o-mini"     # Simple tasks, $0.15/1M input, $0.60/1M output
    capabilities: [reasoning, tool_calling, vision, streaming]
    default_for: [night_audit_review, rate_recommendation]

  - id: "claude"
    models:
      - "claude-sonnet-5"          # Balanced, $3/1M input
      - "claude-haiku-4-5"         # Fast, cheap, $0.80/1M input
    capabilities: [reasoning, tool_calling, vision, streaming, long_context]
    default_for: [front_office_agent, check_in, reservation]

  - id: "deepseek"
    models:
      - "deepseek-v4-pro"          # Strong reasoning, very cost-effective
    capabilities: [reasoning, tool_calling, streaming]
    default_for: [translation, summarization, reporting]

  - id: "local"
    models:
      - "llama-3.1-8b"             # Offline, zero cost, privacy
      - "qwen-2.5-14b"             # Offline, Vietnamese-capable
    capabilities: [basic_chat, translation]
    default_for: [offline_mode, sensitive_data]

  - id: "gemini"
    models:
      - "gemini-2.5-pro"           # Google, multi-modal
    capabilities: [reasoning, vision, streaming, search_grounding]
    default_for: [image_analysis, document_ocr]
```

---

# PHẦN 8: TOOL CALLING ARCHITECTURE

## 8.1. Tool Registry

```csharp
public interface IToolRegistry
{
    void RegisterTool(IAITool tool);
    IAITool GetTool(string toolName);
    IReadOnlyList<IAITool> GetToolsForAgent(string agentId);
    List<ToolDefinition> GetToolDefinitionsForLLM(string agentId);
}

public interface IAITool
{
    string ToolName { get; }              // "check_room_availability"
    string Description { get; }           // For LLM to understand when to use
    ToolParameters Parameters { get; }    // JSON Schema
    string[] RequiredPermissions { get; }
    bool RequiresHumanConfirmation { get; }  // true for financial operations
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, AIContext context);
}

public class ToolDefinition
{
    public string Name { get; set; }
    public string Description { get; set; }
    public ToolParameters Parameters { get; set; }
}

public class ToolParameters
{
    public string Type { get; set; } = "object";
    public Dictionary<string, ToolParameterProperty> Properties { get; set; }
    public string[] Required { get; set; }
}

public class ToolResult
{
    public bool Success { get; set; }
    public string Content { get; set; }        // JSON string
    public string ErrorMessage { get; set; }
}

// ===== TOOL CATALOG (Front Office) =====

public class CheckRoomAvailabilityTool : IAITool
{
    public string ToolName => "check_room_availability";
    public string Description => "Kiểm tra phòng trống theo ngày và loại phòng.";
    public bool RequiresHumanConfirmation => false;

    public ToolParameters Parameters => new()
    {
        Properties = new()
        {
            ["arrival_date"] = new() { Type = "string", Description = "Ngày đến (yyyy-MM-dd)" },
            ["departure_date"] = new() { Type = "string", Description = "Ngày đi (yyyy-MM-dd)" },
            ["room_type"] = new() { Type = "string", Description = "Loại phòng: Standard, Deluxe, Suite" },
            ["adults"] = new() { Type = "integer", Description = "Số người lớn" }
        },
        Required = new[] { "arrival_date", "departure_date" }
    };

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, AIContext context)
    {
        var result = await _roomService.GetAvailableRoomsAsync(
            DateOnly.Parse(args["arrival_date"].ToString()),
            DateOnly.Parse(args["departure_date"].ToString()),
            args.TryGetValue("room_type", out var rt) ? rt?.ToString() : null);

        return new ToolResult { Success = true, Content = JsonSerializer.Serialize(result) };
    }
}

// ===== REQUIRES HUMAN CONFIRMATION =====

public class CheckInStayTool : IAITool
{
    public string ToolName => "check_in_stay";
    public string Description => "Thực hiện check-in cho khách. YÊU CẦU XÁC NHẬN.";
    public bool RequiresHumanConfirmation => true;  // ← FINANCIAL OPERATION

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> args, AIContext context)
    {
        // Before execution, system MUST prompt user:
        // "AI đề xuất check-in khách Nguyễn Văn A vào phòng 101.
        //  Bạn có đồng ý không? [Đồng ý] [Từ chối]"
        var result = await _stayService.CheckInAsync(new CheckInHotelStayCommand(
            Guid.Parse(args["stay_id"].ToString()),
            Convert.FromBase64String(args["row_version"].ToString())));

        return result.IsSuccess
            ? new ToolResult { Success = true, Content = "Check-in thành công." }
            : new ToolResult { Success = false, ErrorMessage = result.Error.Message };
    }
}
```

## 8.2. Tool Permission Matrix

```yaml
Read-Only Tools (no confirmation needed):
  - check_room_availability
  - get_reservation
  - get_guest_profile
  - search_guest
  - get_folio_balance
  - get_room_status
  - get_night_audit_report
  - get_occupancy_forecast
  - get_housekeeping_status
  - translate_text
  - summarize_conversation

Mutation Tools (confirmation required):
  - check_in_stay           ← "AI đề xuất check-in. Bạn đồng ý không?"
  - check_out_stay          ← "AI đề xuất check-out. Bạn đồng ý không?"
  - create_reservation      ← "AI đề xuất tạo đặt phòng. Xác nhận?"
  - receive_payment         ← "AI đề xuất nhận thanh toán. Xác nhận?"
  - assign_room             ← "AI đề xuất phân phòng. Xác nhận?"
  - update_rate             ← "AI đề xuất đổi giá. Xác nhận?"
  - cancel_reservation

Dangerous Tools (Manager Approval Required):
  - close_folio             ← "Yêu cầu phê duyệt của Quản lý"
  - void_payment
  - begin_night_audit
  - rollback_night_audit
```

---

# PHẦN 9: GUARDRAIL SYSTEM

## 9.1. Input Guardrails

```csharp
public interface IGuardrailSystem
{
    Task<GuardrailResult> CheckInputAsync(string userInput, AIRequest request);
    Task<GuardrailResult> CheckOutputAsync(string aiOutput, AIRequest request);
}

public class GuardrailResult
{
    public bool Passed { get; set; }
    public string BlockReason { get; set; }
    public GuardrailViolation Violation { get; set; }
}

public enum GuardrailViolation
{
    None,
    PromptInjection,        // User trying to override system prompt
    Jailbreak,               // User trying to bypass restrictions
    HateSpeech,
    Harassment,
    SexualContent,
    Violence,
    SelfHarm,
    PIILeak,                // AI output contains PII
    FinancialAdvice,        // AI giving unauthorized financial advice
    MedicalAdvice,          // AI giving medical advice (not allowed)
    CompetitorMention,
    OffTopic
}
```

## 9.2. Hallucination Detection

```csharp
public interface IHallucinationDetector
{
    Task<HallucinationResult> DetectAsync(string aiResponse, List<RAGChunk> sources);
}

public class HallucinationResult
{
    public bool IsHallucination { get; set; }
    public float Confidence { get; set; }
    public string SuspiciousClaim { get; set; }
    public string Explanation { get; set; }

    // Detection methods:
    // 1. Factual Consistency: Does response contradict known facts?
    // 2. Source Attribution: Can each claim be traced to a source?
    // 3. Numerical Accuracy: Are numbers consistent with business data?
    // 4. Temporal Consistency: Are dates/times correct?
}
```

---

# PHẦN 10: AI AGENT FRAMEWORK

## 10.1. Agent Definition

```csharp
public interface IAIAgent
{
    string AgentId { get; }
    string DisplayName { get; }
    string Description { get; }
    string SystemPrompt { get; }              // Agent personality and rules
    string DefaultModel { get; }              // Preferred LLM model
    string[] AvailableTools { get; }          // Tool names this agent can use
    string[] RequiredPermissions { get; }
    bool RequiresBusinessContext { get; }
    bool RequiresDomainContext { get; }

    // Agent-specific context enrichment
    Task<AIContext> EnrichContextAsync(AIContext baseContext, CancellationToken ct);
}
```

## 10.2. Agent Catalog

```csharp
// ===== FRONT OFFICE AGENTS =====

public class FrontOfficeAgent : IAIAgent
{
    public string AgentId => "front-office-agent";
    public string DisplayName => "Trợ lý Lễ tân";
    public string SystemPrompt => @"Bạn là trợ lý lễ tân khách sạn ASMILE.
Nhiệm vụ: Hỗ trợ nhân viên lễ tân trong các tác vụ hàng ngày.
Luôn trả lời bằng tiếng Việt, chuyên nghiệp và thân thiện.
Khi đề xuất hành động (check-in, payment...), LUÔN yêu cầu xác nhận.";
    public string[] AvailableTools => new[] {
        "check_room_availability", "get_reservation", "get_guest_profile",
        "check_in_stay", "create_reservation", "calculate_price" };
}

public class NightAuditAgent : IAIAgent
{
    public string AgentId => "night-audit-agent";
    public string DisplayName => "Trợ lý Kiểm đêm";
    public string DefaultModel => "gpt-4o";  // Complex reasoning
    public string SystemPrompt => @"Bạn là trợ lý Night Audit khách sạn ASMILE.
Phân tích báo cáo Night Audit, phát hiện bất thường,
so sánh với dữ liệu lịch sử, và đề xuất hành động.";
    public string[] AvailableTools => new[] {
        "get_night_audit_report", "get_outstanding_balance",
        "get_revenue_snapshot", "get_occupancy_stats" };
}

public class HousekeepingAgent : IAIAgent
{
    public string AgentId => "housekeeping-agent";
    public string DisplayName => "Trợ lý Buồng phòng";
    public string SystemPrompt => @"Bạn là trợ lý buồng phòng.
Tối ưu hóa lịch dọn phòng dựa trên:
- Phòng khách sắp đến (ưu tiên cao nhất)
- Phòng VIP
- Số lượng nhân viên đang làm việc
- Thời gian dọn trung bình mỗi phòng";
    public string[] AvailableTools => new[] {
        "get_dirty_rooms", "get_expected_arrivals",
        "get_housekeeping_staff", "assign_cleaning_task" };
}

public class RevenueAgent : IAIAgent
{
    public string AgentId => "revenue-agent";
    public string DisplayName => "Trợ lý Doanh thu";
    public string DefaultModel => "gpt-4o";
    public string SystemPrompt => @"Bạn là trợ lý Revenue Management.
Phân tích xu hướng đặt phòng, đề xuất điều chỉnh giá,
và dự báo doanh thu. Luôn giải thích lý do cho đề xuất.";
    public string[] AvailableTools => new[] {
        "get_occupancy_forecast", "get_historical_rates",
        "get_booking_pace", "get_market_data" };
}

public class ConciergeAgent : IAIAgent
{
    public string AgentId => "concierge-agent";
    public string DisplayName => "Trợ lý Concierge";
    public string SystemPrompt => @"Bạn là trợ lý concierge.
Gợi ý nhà hàng, điểm tham quan, đặt tour, đặt xe cho khách.";
    public string[] AvailableTools => new[] {
        "search_restaurants", "search_attractions",
        "create_concierge_request", "get_guest_preferences" };
}
```

---

# PHẦN 11: AI WORKFLOW ENGINE

## 11.1. Multi-Step AI Workflow

```csharp
public interface IAIWorkflowEngine
{
    Task<WorkflowResult> ExecuteWorkflowAsync(
        string workflowCode,
        Dictionary<string, object> inputs,
        string userId,
        CancellationToken ct = default);
}

public class AIWorkflowDefinition
{
    public string WorkflowCode { get; set; }
    public string DisplayName { get; set; }
    public List<WorkflowStep> Steps { get; set; }
    public int MaxRetries { get; set; }
    public TimeSpan Timeout { get; set; }
}

public class WorkflowStep
{
    public string StepId { get; set; }
    public string StepType { get; set; }    // "llm_call", "tool_call", "human_approval", "condition"
    public string AgentId { get; set; }
    public string PromptTemplateCode { get; set; }
    public string ToolName { get; set; }
    public Dictionary<string, string> InputMapping { get; set; }
    public string Condition { get; set; }
    public string NextStepOnSuccess { get; set; }
    public string NextStepOnFailure { get; set; }
}
```

## 11.2. Workflow Example — AI-Assisted Check-in

```yaml
workflow_code: "ai_check_in"
steps:
  - id: "find_reservation"
    type: "tool_call"
    tool: "get_reservation"
    description: "Tìm đặt phòng theo tên/số điện thoại khách"

  - id: "verify_guest"
    type: "llm_call"
    agent: "front-office-agent"
    prompt_template: "FO_CHECK_IN_GUEST"
    description: "AI kiểm tra thông tin khách, yêu cầu giấy tờ"

  - id: "check_availability"
    type: "tool_call"
    tool: "check_room_availability"
    description: "Kiểm tra phòng trống phù hợp"

  - id: "recommend_room"
    type: "llm_call"
    agent: "front-office-agent"
    description: "AI đề xuất phòng tối ưu cho khách"

  - id: "human_confirm"
    type: "human_approval"
    description: "Nhân viên xác nhận đề xuất của AI"

  - id: "execute_checkin"
    type: "tool_call"
    tool: "check_in_stay"
    description: "Thực hiện check-in (sau khi có xác nhận)"

  - id: "create_folio"
    type: "tool_call"
    tool: "create_folio"
    description: "Tự động tạo folio cho stay mới"

  - id: "welcome_message"
    type: "llm_call"
    agent: "front-office-agent"
    description: "AI soạn tin nhắn chào mừng gửi khách"
```

---

# PHẦN 12: OBSERVABILITY

## 12.1. AI Audit Trail

```csharp
public class AIAuditEntry
{
    public Guid AuditId { get; set; }
    public string UserId { get; set; }
    public string AgentId { get; set; }
    public string Intent { get; set; }
    public string UserInput { get; set; }          // Original user message
    public string FinalPrompt { get; set; }         // Complete prompt sent to LLM
    public string AIResponse { get; set; }
    public string ModelUsed { get; set; }
    public int TokensPrompt { get; set; }
    public int TokensCompletion { get; set; }
    public decimal Cost { get; set; }
    public TimeSpan Latency { get; set; }
    public List<ToolCallRecord> ToolCalls { get; set; }
    public List<string> RAGSources { get; set; }
    public float Confidence { get; set; }
    public string ReasoningTrace { get; set; }
    public string UserFeedback { get; set; }        // Thumbs up/down
    public DateTime OccurredAtUtc { get; set; }
}

// AI Audit entries are immutable. Full history searchable.
// Every AI interaction is traceable end-to-end.
```

## 12.2. AI Metrics Dashboard

```yaml
Operational Metrics:
  - Total AI requests (daily, weekly, monthly)
  - Requests per agent
  - Average latency per model
  - Token usage per model
  - Cost per model / per agent / per user
  - Error rate per model
  - Hallucination rate
  - Tool call success rate

Quality Metrics:
  - User feedback (thumbs up/down ratio)
  - Human confirmation accept rate (AI suggestions accepted)
  - RAG relevance score
  - Response completeness score
  - Factual accuracy score

Business Impact:
  - Time saved per task (AI-assisted vs manual)
  - Revenue uplift from AI recommendations
  - Guest satisfaction improvement
  - Staff productivity improvement
```

---

# PHẦN 13: FOLDER STRUCTURE

```
ASMILE.BO.AI/
├── ASMILE.BO.AI.Abstractions/
│   ├── IAIOrchestrator.cs
│   ├── IAIAgent.cs
│   ├── IPromptRegistry.cs
│   ├── IContextEngine.cs
│   ├── IMemoryEngine.cs
│   ├── IRAGPipeline.cs
│   ├── ILLMGateway.cs
│   ├── IModelRouter.cs
│   ├── IToolRegistry.cs
│   ├── IAITool.cs
│   ├── IGuardrailSystem.cs
│   ├── IHallucinationDetector.cs
│   ├── IAIWorkflowEngine.cs
│   └── IAIAuditService.cs
│
├── ASMILE.BO.AI.Orchestrator/
│   ├── AIOrchestrator.cs
│   └── OrchestratorPipeline.cs
│
├── ASMILE.BO.AI.PromptEngine/
│   ├── PromptRegistry.cs
│   └── PromptTemplates/
│       ├── FrontOfficePrompts.cs
│       ├── HousekeepingPrompts.cs
│       └── NightAuditPrompts.cs
│
├── ASMILE.BO.AI.ContextEngine/
│   └── ContextAssembler.cs
│
├── ASMILE.BO.AI.MemoryEngine/
│   ├── ConversationMemory.cs
│   └── SemanticMemory.cs
│
├── ASMILE.BO.AI.RAG/
│   ├── RAGPipeline.cs
│   ├── EmbeddingService.cs
│   └── RetrieverService.cs
│
├── ASMILE.BO.AI.ModelRouter/
│   ├── ModelRouter.cs
│   ├── LLMGateway.cs
│   └── Providers/
│       ├── OpenAIProvider.cs
│       ├── ClaudeProvider.cs
│       ├── DeepSeekProvider.cs
│       └── LocalLLMProvider.cs
│
├── ASMILE.BO.AI.Tools/
│   ├── ToolRegistry.cs
│   └── Tools/
│       ├── CheckRoomAvailabilityTool.cs
│       ├── CheckInStayTool.cs
│       ├── CreateReservationTool.cs
│       └── ...
│
├── ASMILE.BO.AI.Agents/
│   ├── FrontOfficeAgent.cs
│   ├── NightAuditAgent.cs
│   ├── HousekeepingAgent.cs
│   ├── RevenueAgent.cs
│   └── ConciergeAgent.cs
│
├── ASMILE.BO.AI.Guardrails/
│   ├── GuardrailSystem.cs
│   └── HallucinationDetector.cs
│
├── ASMILE.BO.AI.Workflow/
│   ├── AIWorkflowEngine.cs
│   └── Workflows/
│       └── AiCheckInWorkflow.cs
│
└── ASMILE.BO.AI.Observability/
    ├── AIAuditService.cs
    └── AIMetricsService.cs
```

---

# PHẦN 14: IMPLEMENTATION ROADMAP

| Sprint | Deliverable | Duration |
|---|---|---|
| AI-001 | AI Abstractions (all interfaces) | 2 ngày |
| AI-002 | LLM Gateway + OpenAI + Claude providers | 3 ngày |
| AI-003 | Model Router + Cost optimization | 2 ngày |
| AI-004 | Prompt Registry + Prompt Templates (10+) | 3 ngày |
| AI-005 | Context Engine (Business + User + Domain) | 2 ngày |
| AI-006 | Memory Engine (Conversation + Semantic) | 2 ngày |
| AI-007 | RAG Pipeline + Embedding + Knowledge Base | 3 ngày |
| AI-008 | Tool Registry + 15 Tools (Front Office) | 3 ngày |
| AI-009 | AI Agents (5 agents: FO, HK, NA, Revenue, Concierge) | 3 ngày |
| AI-010 | AI Orchestrator + 10-step pipeline | 3 ngày |
| AI-011 | Guardrail System + Hallucination Detection | 2 ngày |
| AI-012 | AI Workflow Engine + AI Check-in workflow | 2 ngày |
| AI-013 | AI Audit + Observability + Metrics | 2 ngày |
| AI-014 | AI Assistant UI (WPF Chat panel) | 3 ngày |
| AI-015 | AI Copilot integration (inline suggestions) | 3 ngày |
| AI-016 | Integration testing + Prompt evaluation | 3 ngày |
| AI-017 | Production deployment + Monitoring | 2 ngày |

**Total: ~43 working days (8.5 weeks)**

---

# PHẦN 15: ARCHITECTURE DECISION RECORDS

### ADR-AI001: Model Agnostic Architecture
**Decision:** All AI interactions go through LLMGateway abstraction. Business logic never calls OpenAI/Claude/DeepSeek directly.
**Rationale:** Model landscape changes rapidly. Switching providers must be configuration change, not code change. Enables cost optimization via Model Router.

### ADR-AI002: AI Assists, Never Controls
**Decision:** AI can suggest actions but never executes financial or operational mutations without explicit human confirmation.
**Rationale:** Hotel PMS is a financial system. AI errors in billing, check-in, or pricing have real monetary consequences. Human-in-the-loop for all mutations.

### ADR-AI003: Prompt Templates as Versioned Assets
**Decision:** All system prompts are stored in Prompt Registry with versioning, testing, and approval workflow. No prompts hardcoded in C#.
**Rationale:** Prompt engineering is iterative. Versioning enables A/B testing, rollback, and auditing. Non-technical staff can review and approve prompts.

### ADR-AI004: RAG over Fine-tuning (for now)
**Decision:** Use RAG (Retrieval-Augmented Generation) for domain knowledge. Defer model fine-tuning to post-MVP.
**Rationale:** RAG is simpler to implement, update, and audit. Knowledge base changes take effect immediately without re-training. Fine-tuning is appropriate when we have 10,000+ high-quality examples.

### ADR-AI005: Multi-Provider with Local Fallback
**Decision:** Support at least 4 cloud providers (OpenAI, Claude, DeepSeek, Gemini) + 1 local model (Llama/Qwen) for offline/privacy.
**Rationale:** No single point of failure. Cost optimization via routing. Local model for sensitive data and offline operation.

---

# PHẦN 16: CODING STANDARDS (AI)

```yaml
Prompt Management:
  - All prompts in Prompt Registry, not in code
  - Prompt template variables use {{double_braces}}
  - Every prompt has: version, status, test cases, approval
  - Prompts reviewed by domain experts, not just engineers

Tool Design:
  - Tool name: snake_case, descriptive (check_room_availability)
  - Tool description: written for LLM to understand when to call
  - Parameters: JSON Schema, not just Dictionary
  - Mutation tools: RequiresHumanConfirmation = true
  - Tool must NEVER expose raw database access

AI Agent Design:
  - SystemPrompt: clear role, rules, boundaries
  - AvailableTools: explicit allowlist (not all tools)
  - DefaultModel: appropriate for agent complexity
  - Context enrichment: load domain data BEFORE prompting

Security:
  - PII never sent to external LLM (mask before sending)
  - All external LLM calls logged (prompt, response, tokens, cost)
  - Rate limiting per user, per agent, per model
  - Prompt injection detection on every user input

Evaluation:
  - Every prompt template has test cases
  - Weekly automated evaluation of all prompts
  - User feedback collected on every AI response
  - Hallucination rate tracked per prompt template
```

---

*Enterprise AI Platform architecture complete. 16 sections. This is the AI Kernel for the entire ASMILE ERP ecosystem — every AI assistant, agent, workflow, and automation runs on this platform.*
