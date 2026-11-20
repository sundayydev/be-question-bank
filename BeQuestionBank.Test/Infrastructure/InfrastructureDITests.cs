namespace BeQuestionBank.Test.Infrastructure;

using System.Collections.Generic;
using BeQuestionBank.Application.Common.Interfaces;
using BeQuestionBank.Application.Common.Interfaces.Services;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class InfrastructureDITests
{
    [Fact]
    public void AddInfrastructureServices_ShouldRegisterExpectedServices()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            {"ConnectionStrings:PostgresConnection", "Host=localhost;Database=dummy"},
            {"ConnectionStrings:Redis", "localhost:6379,abortConnect=false"}
        }).Build();

        services.AddLogging();
        services.AddInfrastructureServices(config);

        Assert.Contains(services, s => s.ServiceType == typeof(IKhoaRepository));
        Assert.Contains(services, s => s.ServiceType == typeof(IImportService));
        Assert.Contains(services, s => s.ServiceType == typeof(IEncryptionService));
        Assert.Contains(services, s => s.ServiceType == typeof(IApplicationDbContext));
    }
}
