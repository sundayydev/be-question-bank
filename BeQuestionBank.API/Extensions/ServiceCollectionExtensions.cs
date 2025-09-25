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
        
        //DeThi
        services.AddScoped<DeThiService>();
        services.AddScoped<IDeThiRepository, DeThiRepository>();
        
        //YeuCauRutTrich
        services.AddScoped<YeuCauRutTrichService>();
        services.AddScoped<IYeuCauRutTrichRepository,YeuCauRutTrichRepository>();
        services.AddScoped<CauHoiRepository>();
        services.AddScoped<ICauHoiRepository,CauHoiRepository>();
       
        return services;
    }
}

