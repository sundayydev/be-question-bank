using BeQuestionBank.Core.Configurations;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.DeThi;
using BEQuestionBank.Shared.DTOs.DeThi;
using Microsoft.EntityFrameworkCore;

namespace BEQuestionBank.Core.Repositories;

public class DeThiRepository : GenericRepository<DeThi>, IDeThiRepository
{
    private readonly AppDbContext _context;

    public DeThiRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<object> GetByIdWithChiTietAsync(Guid id)
    {
        var deThi = await _context.DeThis
            .Include(dt => dt.ChiTietDeThis)
            .Include(dt => dt.MonHoc)
            .ThenInclude(mh => mh.Khoa)
            .FirstOrDefaultAsync(dt => dt.MaDeThi == id);

        return deThi;
    }


    public async  Task<IEnumerable<object>> GetAllWithChiTietAsync()
    {
        var deThi = await _context.DeThis
            .Include(dt => dt.ChiTietDeThis)
            .Include(dt => dt.MonHoc)
            .ThenInclude(mh => mh.Khoa)
            .ToListAsync();
        return deThi;
    }

    public async Task<IEnumerable<object>> GetAllBasicAsync()
    {
        var deThis = await _context.DeThis
            .Include(d => d.MonHoc)
            .AsNoTracking()
            .ToListAsync();
        return deThis;
    }


    public Task<object> UpdateWithChiTietAsync(DeThiDto deThiDto)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<DeThi>> GetByMaMonHocAsync(Guid maMonHoc)
    {
        return await _context.DeThis
            .Where(d => d.MaMonHoc == maMonHoc)
            .Include(d => d.MonHoc)
            .ThenInclude(m => m.Khoa)
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<IEnumerable<object>> GetApprovedDeThisAsync()
    {
        return await _context.DeThis
            .Where(d => d.DaDuyet)
            .Include(d => d.MonHoc)
            .Include(d => d.ChiTietDeThis)
            .ToListAsync();

    }

    public async Task<object> GetDeThiWithChiTietAndCauTraLoiAsync(Guid maDeThi)
    {
        return await _context.DeThis
            .Include(dt => dt.MonHoc)
            .ThenInclude(mh => mh.Khoa)
            .Include(dt => dt.ChiTietDeThis)
            .ThenInclude(ct => ct.CauHoi)
            .ThenInclude(ch => ch.CauTraLois)
            .Include(dt => dt.ChiTietDeThis)
            .ThenInclude(ct => ct.CauHoi)
            .ThenInclude(ch => ch.CauHoiCons)
            .ThenInclude(chc => chc.CauTraLois)
            .FirstOrDefaultAsync(dt => dt.MaDeThi == maDeThi);
    }
    public async Task<DeThi?> GetFullForExportAsync(Guid maDeThi)
    {
        return await _context.DeThis
            .Include(d => d.MonHoc)
                .ThenInclude(m => m.Khoa)
            .Include(d => d.ChiTietDeThis!)
                .ThenInclude(ct => ct.Phan)
            .Include(d => d.ChiTietDeThis!)
                .ThenInclude(ct => ct.CauHoi!)
                    .ThenInclude(ch => ch.CauTraLois)
            .Include(d => d.ChiTietDeThis!)
                .ThenInclude(ct => ct.CauHoi!)
                    .ThenInclude(ch => ch.CauHoiCons)
                        .ThenInclude(chc => chc.CauTraLois)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.MaDeThi == maDeThi);
    }
}