using BeQuestionBank.Core.Configurations;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
