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

public class KhoaRepository(AppDbContext context) : GenericRepository<Khoa>(context), IKhoaRepository
{
    private new readonly AppDbContext _context = context;

    public async Task<Khoa?> GetByTenKhoaAsync(string tenKhoa)
    {
        return await _context.Khoas.FirstOrDefaultAsync(k => k.TenKhoa == tenKhoa);
    }

    public async override Task<IEnumerable<Khoa>> GetAllAsync()
    {
        return await _context.Khoas.Include(k => k.MonHocs).ToListAsync();
    }
}

