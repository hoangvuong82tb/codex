using ASMILE.BO.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.Tests.Unit;

public sealed class InfrastructureConfigurationTests
{
    [Fact]
    public void INFRA_CFG_001_requires_a_connection_string_when_no_secret_is_configured()
    {
        var original = Environment.GetEnvironmentVariable("ASMILE_BO_CONNECTION_STRING");
        try
        {
            Environment.SetEnvironmentVariable("ASMILE_BO_CONNECTION_STRING", null);
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ConnectionString"] = ""
                })
                .Build();

            var services = new ServiceCollection();

            Action act = () => services.AddInfrastructure(configuration);

            act.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("*connection*not configured*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ASMILE_BO_CONNECTION_STRING", original);
        }
    }
}
