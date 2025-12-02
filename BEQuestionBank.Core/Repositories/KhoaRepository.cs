using BeQuestionBank.Core.Configurations;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace BEQuestionBank.Core.Repositories;

public class KhoaRepository(AppDbContext context) : GenericRepository<Khoa>(context), IKhoaRepository
{
    private new readonly AppDbContext _context = context;

    public async Task<Khoa?> GetByTenKhoaAsync(string tenKhoa)
    {
        return await _context.Khoas.FirstOrDefaultAsync(k => k.TenKhoa == tenKhoa);
    }

    public async override Task<IEnumerable<Khoa>> GetAllAsync()
    {
        return await _context.Khoas 
            .Include(k => k.MonHocs)
            .ToListAsync();
    }
    public async Task<PagedResult<KhoaDto>> GetTrashedAsync(int page = 1, int pageSize = 10)
    {
        var query = _context.Khoas
            .AsNoTracking()
            .Where(k => k.XoaTam == true)
            .OrderByDescending(k => k.NgayCapNhat);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(k => new KhoaDto
            {
                MaKhoa = k.MaKhoa,
                TenKhoa = k.TenKhoa,
                MoTa = k.MoTa,
                NgayCapNhat = k.NgayCapNhat,
                XoaTam = k.XoaTam
            })
            .ToListAsync();

        return new PagedResult<KhoaDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}

