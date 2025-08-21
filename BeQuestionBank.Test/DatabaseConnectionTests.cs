using BeQuestionBank.Core.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace BeQuestionBank.Test;

public class DatabaseConnectionTests
{
    [Fact]
    public async Task CanConnectToDatabase()
    {
        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("PostgresConnection");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        using var context = new AppDbContext(options);
        var canConnect = await context.Database.CanConnectAsync();

        Assert.True(canConnect);
    }
}
