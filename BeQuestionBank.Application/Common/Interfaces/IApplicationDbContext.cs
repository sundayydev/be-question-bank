namespace BeQuestionBank.Application.Common.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using BeQuestionBank.Domain.Models;
using Microsoft.EntityFrameworkCore;
using File = BeQuestionBank.Domain.Models.File;

public interface IApplicationDbContext
{
    DbSet<Khoa> Khoas { get; }
    DbSet<MonHoc> MonHocs { get; }
    DbSet<Phan> Phans { get; }
    DbSet<NguoiDung> NguoiDungs { get; }
    DbSet<CauHoi> CauHois { get; }
    DbSet<CauTraLoi> CauTraLois { get; }
    DbSet<DeThi> DeThis { get; }
    DbSet<ChiTietDeThi> ChiTietDeThis { get; }
    DbSet<YeuCauRutTrich> YeuCauRutTrichs { get; }
    DbSet<File> Files { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
