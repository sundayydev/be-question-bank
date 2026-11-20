using BeQuestionBank.Core.Configurations;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.DTOs.Phan;

namespace BEQuestionBank.Core.Repositories;

public class PhanRepository(AppDbContext context) : GenericRepository<Phan>(context), IPhanRepository
{
    private new readonly AppDbContext _context = context;

    public async Task<IEnumerable<Phan?>> GetByMaMonHocAsync(Guid maMonHoc)
    {
        return await _context.Phans
         .Where(p => p.MaMonHoc == maMonHoc)
         .OrderBy(p => p.ThuTu)   // sắp xếp tăng dần theo thứ tự
         .ToListAsync();
    }

    public async Task<List<Phan>> GetSiblingsAsync(Guid maMonHoc, Guid? maPhanCha)
    {
        return await _context.Phans
            .Where(p => p.MaMonHoc == maMonHoc && p.MaPhanCha == maPhanCha)
            .ToListAsync();
    }

    public async Task<PagedResult<PhanDto>> GetTrashedAsync(int page = 1, int pageSize = 10)
    {
        var query = _context.Phans.Include(x => x.MonHoc)
            .AsNoTracking()
            .Where(k => k.XoaTam == true)
            .OrderByDescending(k => k.NgayCapNhat);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(k => new PhanDto()
            {
                MaPhan = k.MaPhan,
                ThuTu = k.ThuTu,
                TenPhan = k.TenPhan,
                TenMonHoc = k.MonHoc.TenMonHoc, 
                NgayCapNhat = k.NgayCapNhat,
                MaMonHoc = k.MaMonHoc,
                MaPhanCha = k.MaPhanCha,
                NoiDung = k.NoiDung,
                SoLuongCauHoi = k.SoLuongCauHoi,
                NgayTao = k.NgayTao,
                
            })
            .ToListAsync();

        return new PagedResult<PhanDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
