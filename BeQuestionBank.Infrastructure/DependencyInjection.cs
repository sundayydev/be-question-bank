namespace BeQuestionBank.Infrastructure;

using BE_CIRRO.Core.Services;
using BeQuestionBank.Application.Common.Interfaces;
using BeQuestionBank.Application.Common.Interfaces.Services;
using BeQuestionBank.Core.Configurations;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BEQuestionBank.Core.Helpers;
using BEQuestionBank.Core.Repositories;
using BEQuestionBank.Core.Services;
using BEQuestionBank.Domain.Interfaces.Repo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("PostgresConnection"),
                x => x.MigrationsAssembly("BeQuestionBank.Infrastructure")
            ));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Redis
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConn))
        {
            try
            {
                services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConn));
            }
            catch
            {
                // Fallback for testing environments where Redis may be unreachable
            }
        }

        // Repositories
        services.AddScoped<IKhoaRepository, KhoaRepository>();
        services.AddScoped<KhoaRepository>();
        services.AddScoped<IMonHocRepository, MonHocRepository>();
        services.AddScoped<MonHocRepository>();
        services.AddScoped<IPhanRepository, PhanRepository>();
        services.AddScoped<PhanRepository>();
        services.AddScoped<IDeThiRepository, DeThiRepository>();
        services.AddScoped<DeThiRepository>();
        services.AddScoped<ICauHoiRepository, CauHoiRepository>();
        services.AddScoped<CauHoiRepository>();
        services.AddScoped<INguoiDungRepository, NguoiDungRepository>();
        services.AddScoped<NguoiDungRepository>();
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<FileRepository>();
        services.AddScoped<IYeuCauRutTrichRepository, YeuCauRutTrichRepository>();
        services.AddScoped<YeuCauRutTrichRepository>();

        // Service Abstractions
        services.AddScoped<IJwtService, JwtHelper>();
        services.AddScoped<IRedisCacheService, RedisService>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IEncryptionService, EzpEncryptionService>();
        services.AddScoped<IFileStorageService, FileService>();
        services.AddScoped<IExportService, DeThiExportService>();
        services.AddScoped<IToolService, ToolService>();

        // Concrete services (for direct use and backward-compatibility)
        services.AddScoped<KhoaService>();
        services.AddScoped<MonHocService>();
        services.AddScoped<PhanService>();
        services.AddScoped<DeThiService>();
        services.AddScoped<YeuCauRutTrichService>();
        services.AddScoped<NguoiDungService>();
        services.AddScoped<AuthService>();
        services.AddScoped<ImportService>();
        services.AddScoped<CauHoiService>();
        services.AddScoped<FileService>();
        services.AddScoped<ToolService>();
        services.AddScoped<RutTrichTuLuanService>();
        services.AddScoped<DeThiExportForStudentService>();
        services.AddScoped<DeThiTuLuanExportService>();
        services.AddScoped<DeThiExportService>();
        services.AddScoped<EzpEncryptionService>();
        services.AddScoped<JwtHelper>();
        services.AddScoped<RedisService>();

        return services;
    }
}
