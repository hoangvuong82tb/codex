using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ASMILE.BO.Tests.Integration;

/// <summary>
/// SQL Server integration tests create and migrate isolated databases on the
/// same LocalDB instance. The complete integration assembly is serialized so
/// unrelated test collections cannot interfere with shared EF/LocalDB state.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SqlServerIntegrationCollection
{
    public const string Name = "SQL Server integration";
}
