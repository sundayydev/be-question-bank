using BE_CIRRO.Core.Services;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BEQuestionBank.Core.Helpers;
using BEQuestionBank.Core.Repositories;
using BEQuestionBank.Core.Services;
using BEQuestionBank.Domain.Interfaces.Repo;

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
        services.AddScoped<DeThiExportForStudentService>();
        
        //nguoidung
        services.AddScoped<NguoiDungService>();
        services.AddScoped<INguoiDungRepository,NguoiDungRepository>();

        services.AddScoped<JwtHelper>();
        services.AddScoped<RedisService>();
        services.AddScoped<AuthService>();

        services.AddScoped<ImportService>();

        services.AddScoped<CauHoiService>();
        services.AddScoped<ICauHoiRepository, CauHoiRepository>();
        
        //File
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<FileService>();

        return services;
    }
}

