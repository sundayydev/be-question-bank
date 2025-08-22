using BeQuestionBank.Domain.Interfaces.IRepositories;
using BEQuestionBank.Core.Repositories;
using BEQuestionBank.Core.Services;

namespace BeQuestionBank.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        //Khoa
        services.AddScoped<KhoaService>();
        services.AddScoped<IKhoaRepository, KhoaRepository>();

        //MonHoc
        services.AddScoped<MonHocService>();
        services.AddScoped<IMonHocRepository, MonHocRepository>();

        //Phan
        services.AddScoped<PhanService>();
        services.AddScoped<IPhanRepository, PhanRepository>();

        return services;
    }
}

