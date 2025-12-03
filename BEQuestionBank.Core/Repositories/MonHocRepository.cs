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
    }
}
