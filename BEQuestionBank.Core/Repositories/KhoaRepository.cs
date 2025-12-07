using BeQuestionBank.Core.Configurations;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BEQuestionBank.Core.Common;
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

    // public async Task<PagedResult<KhoaDto>> GetPagedKhoasAsync(int page, int pageSize, string? sort = null, string? search = null)
    // {
    //     var query = _context.Khoas.Include(k => k.MonHocs).Where(k => k.XoaTam==false);
    //
    //     if (!string.IsNullOrWhiteSpace(search))
    //     {
    //         var lowerSearch = search.ToLower();
    //         query = query.Where(k => k.TenKhoa.ToLower().Contains(lowerSearch));
    //     }
    //
    //     return await query.ToPagedResultAsync(page, pageSize, sort);
    // }
    public async Task<PagedResult<KhoaDto>> GetPagedKhoasAsync(
        int page,
        int pageSize,
        string? sort = null,
        string? search = null
    )
    {
        var query = _context.Khoas
            .AsNoTracking()
            .Include(k => k.MonHocs)
            .Where(k => k.XoaTam == false);

        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(k => k.TenKhoa.ToLower().Contains(searchLower));
        }

        // Default sort
        query = query.OrderBy(k => k.TenKhoa);

        // Custom sort nếu có truyền lên: format "TenKhoa,desc" hoặc "TenKhoa"
        if (!string.IsNullOrWhiteSpace(sort))
        {
            var parts = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
            var column = parts[0].Trim();
            var direction = parts.Length > 1 ? parts[1].Trim().ToLower() : "asc";

            query = (column.ToLower(), direction) switch
            {
                ("tenkhoa", "desc") => query.OrderByDescending(k => k.TenKhoa),
                ("tenkhoa", "asc") => query.OrderBy(k => k.TenKhoa),
                ("ngaytao", "desc") => query.OrderByDescending(k => k.NgayTao),
                ("ngaytao", "asc") => query.OrderBy(k => k.NgayTao),
                _ => query.OrderBy(k => k.TenKhoa)
            };
        }

        // Project to DTO trước khi phân trang (tối ưu performance)
        var projectedQuery = query.Select(k => new KhoaDto
        {
            MaKhoa = k.MaKhoa,
            TenKhoa = k.TenKhoa,
            MoTa = k.MoTa,
            XoaTam = k.XoaTam,
            NgayTao = k.NgayTao,
            NgayCapNhat = k.NgayCapNhat,
        });

        return await projectedQuery.ToPagedResultAsync(page, pageSize);
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

    public async Task<Khoa?> GetByIdKhoaAsync(Guid id)
    {
        return await _context.Khoas
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.MaKhoa == id && (k.XoaTam == false || k.XoaTam == false));
    }
}