using BeQuestionBank.Core.Configurations;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BEQuestionBank.Core.Repositories;

public class YeuCauRutTrichRepository  : GenericRepository<YeuCauRutTrich> , IYeuCauRutTrichRepository
{
    private readonly AppDbContext _context;
    public YeuCauRutTrichRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<YeuCauRutTrich>> GetByMaNguoiDungAsync(Guid maNguoiDung)
    {
        return await _context.YeuCauRutTrichs
            .Include(y => y.NguoiDung)
            .Include(y => y.MonHoc)
            .Where(y => y.MaNguoiDung == maNguoiDung)
            .ToListAsync();
    }

    public async Task<IEnumerable<YeuCauRutTrich>> GetByMaMonHocAsync(Guid maMonHoc)
    {
        return await _context.YeuCauRutTrichs
            .Include(y => y.NguoiDung)
            .Include(y => y.MonHoc)
            .Where(y => y.MaMonHoc == maMonHoc)
            .ToListAsync();
    }

    public async Task<IEnumerable<YeuCauRutTrich>> GetByTrangThaiAsync(bool? daXuLy)
    {
        return await _context.YeuCauRutTrichs
            .Include(y => y.NguoiDung)
            .Include(y => y.MonHoc)
            .Where(y => y.DaXuLy == daXuLy)
            .ToListAsync();
    }
}