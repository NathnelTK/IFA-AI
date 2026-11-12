using IFA.Infrastructure;
using IFA.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IFA.UnitTests;

public sealed class InfrastructureRegistrationTests
{
    [Fact]
    public void ApiInfrastructureDoesNotRegisterBackgroundWorkers()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddInfrastructure("Data Source=:memory:", Configuration());

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public void WorkerInfrastructureRegistersGenerationAndOutboxServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configuration = Configuration();
        services.AddInfrastructure("Data Source=:memory:", configuration);

        services.AddWorkerInfrastructure(configuration);

        var hostedImplementations = services
            .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
            .Select(descriptor => descriptor.ImplementationType)
            .ToArray();
        Assert.Contains(typeof(ModuleGenerationBackgroundService), hostedImplementations);
        Assert.Contains(typeof(OutboxDispatcherBackgroundService), hostedImplementations);
        Assert.Contains(typeof(AuthoringJobBackgroundService), hostedImplementations);
        Assert.Contains(typeof(AssetProcessingBackgroundService), hostedImplementations);
        Assert.Contains(typeof(OperationalMaintenanceBackgroundService), hostedImplementations);
        Assert.Contains(typeof(DomainVerificationBackgroundService), hostedImplementations);
        Assert.Contains(typeof(CertificateRenderBackgroundService), hostedImplementations);
        Assert.Contains(typeof(PaymentReconciliationBackgroundService), hostedImplementations);
        Assert.Equal(8, hostedImplementations.Length);
    }

    private static IConfiguration Configuration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DB_PROVIDER"] = "sqlite",
                ["Workers:Generation:LeaseSeconds"] = "120",
                ["Workers:Generation:HeartbeatSeconds"] = "30"
            })
            .Build();
}
