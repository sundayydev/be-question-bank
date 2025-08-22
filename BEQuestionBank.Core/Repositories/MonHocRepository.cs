using BeQuestionBank.Core.Configurations;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BEQuestionBank.Core.Repositories
{
    public class MonHocRepository(AppDbContext context) : GenericRepository<MonHoc>(context), IMonHocRepository
    {
        private new readonly AppDbContext _context = context;

        public async Task<IEnumerable<MonHoc?>> GetByMaKhoaAsync(Guid maKhoa)
        {
            return await _context.MonHocs.Where(m => m.MaKhoa == maKhoa).ToListAsync();
        }
    }
}
