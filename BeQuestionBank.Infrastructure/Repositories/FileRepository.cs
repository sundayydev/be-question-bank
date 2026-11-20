using BeQuestionBank.Core.Configurations;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using File = BeQuestionBank.Domain.Models.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BEQuestionBank.Core.Repositories
{
    public class FileRepository(AppDbContext context) : GenericRepository<File>(context), IFileRepository
    {
        private new readonly AppDbContext _context = context;

        public async Task<IEnumerable<File?>> FindFilesByCauHoiAsync(Guid maCauHoi)
        {
            return await _context.Files
                .Where(f => f.MaCauHoi == maCauHoi)
                .ToListAsync();
        }

        public async Task<IEnumerable<File?>> FindFilesByCauTraLoiAsync(Guid maCauTraLoi)
        {
            return await _context.Files
                .Where(f => f.MaCauTraLoi == maCauTraLoi)
                .ToListAsync();
        }

    }
}
