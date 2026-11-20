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
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace BEQuestionBank.Core.Repositories
{
    public class MonHocRepository(AppDbContext context) : GenericRepository<MonHoc>(context), IMonHocRepository
    {
        private new readonly AppDbContext _context = context;

        public async Task<IEnumerable<MonHoc?>> GetByMaKhoaAsync(Guid maKhoa)
        {
            return await _context.MonHocs.Where(m => m.MaKhoa == maKhoa).ToListAsync();
        }
        public async override Task<IEnumerable<MonHoc>> GetAllAsync()
        {
            return await _context.MonHocs 
                .Include(k => k.Khoa)
                .ToListAsync();
        }
        public async Task<PagedResult<MonHocDto>> GetTrashedAsync(int page = 1, int pageSize = 10)
        {
            var query = _context.MonHocs
                .AsNoTracking()
                .Where(k => k.XoaTam == true)
                .OrderByDescending(k => k.NgayCapNhat);

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(k => new MonHocDto()
                {
                    MaMonHoc = k.MaMonHoc,
                    MaSoMonHoc = k.MaSoMonHoc,
                    MaKhoa = k.MaKhoa,
                    TenMonHoc = k.TenMonHoc,
                    SoTinChi = k.SoTinChi,
                    NgayCapNhat = k.NgayCapNhat,
                    XoaTam = k.XoaTam
                })
                .ToListAsync();

            return new PagedResult<MonHocDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PagedResult<MonHocDto>> GetPagedMonHocsAsync(
            int page, int pageSize, Guid? maKhoa = null, string? search = null, string? sort = null)
        {
            var query = _context.MonHocs
                .AsNoTracking()
                .Where(m => m.XoaTam == false);

            if (maKhoa.HasValue)
            {
                query = query.Where(m => m.MaKhoa == maKhoa.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(m =>
                    m.TenMonHoc.ToLower().Contains(lowerSearch) ||
                    m.MaSoMonHoc.ToLower().Contains(lowerSearch));
            }

            if (!string.IsNullOrWhiteSpace(sort))
            {
                var parts = sort.Split(',');
                var column = parts[0];
                var direction = parts.Length > 1 ? parts[1].ToLower() : "asc";

                query = column switch
                {
                    "TenMonHoc" when direction == "desc" => query.OrderByDescending(m => m.TenMonHoc),
                    "TenMonHoc" => query.OrderBy(m => m.TenMonHoc),
                    "MaSoMonHoc" when direction == "desc" => query.OrderByDescending(m => m.MaSoMonHoc),
                    "MaSoMonHoc" => query.OrderBy(m => m.MaSoMonHoc),
                    _ => query.OrderBy(m => m.TenMonHoc)
                };
            }
            else
            {
                query = query.OrderBy(m => m.TenMonHoc);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MonHocDto
                {
                    MaMonHoc = m.MaMonHoc,
                    MaSoMonHoc = m.MaSoMonHoc,
                    TenMonHoc = m.TenMonHoc,
                    MaKhoa = m.MaKhoa,
                    SoTinChi = m.SoTinChi,
                    NgayCapNhat = m.NgayCapNhat,
                    XoaTam = m.XoaTam
                })
                .ToListAsync();

            return new PagedResult<MonHocDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
