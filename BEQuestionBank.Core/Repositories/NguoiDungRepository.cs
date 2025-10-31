
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using BeQuestionBank.Core.Configurations;
using BEQuestionBank.Domain.Interfaces.Repo;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace BEQuestionBank.Core.Repositories
{
    public class NguoiDungRepository : GenericRepository<NguoiDung>, INguoiDungRepository
    {
        private readonly AppDbContext _context;

        public NguoiDungRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<NguoiDung> GetByIdAsync(Guid maNguoiDung)
        {
            return await _context.NguoiDungs.Include(nd => nd.Khoa)
                .AsNoTracking()
                .FirstOrDefaultAsync(nd => nd.MaNguoiDung == maNguoiDung);
        }

        public async Task<NguoiDung> GetByUsernameAsync(string tenDangNhap)
        {
            return await _context.NguoiDungs
                .AsNoTracking()
                .FirstOrDefaultAsync(nd => nd.TenDangNhap == tenDangNhap);
        }
        
        public async Task<IEnumerable<NguoiDung>> GetByVaiTroAsync(EnumRole vaiTro)
        {
            return await _context.NguoiDungs
                .AsNoTracking()
                .Where(nd => nd.VaiTro == vaiTro)
                .ToListAsync();
        }

        public async Task<IEnumerable<NguoiDung>> GetByKhoaAsync(Guid maKhoa)
        {
            return await _context.NguoiDungs
                .AsNoTracking()
                .Where(nd => nd.MaKhoa == maKhoa)
                .ToListAsync();
        }

        public async Task<bool> IsLockedAsync(string tenDangNhap)
        {
            return await _context.NguoiDungs
                .AsNoTracking()
                .Where(nd => nd.TenDangNhap == tenDangNhap)
                .Select(nd => nd.BiKhoa)
                .FirstOrDefaultAsync();
        }

        public async Task<NguoiDung> GetByResetCodeAsync(Guid maKhoa)
        {
            return await _context.NguoiDungs
                .AsNoTracking()
                .FirstOrDefaultAsync(nd => nd.MaKhoa == maKhoa);
        }

        public async Task<IEnumerable<NguoiDung>> GetAllAsync()
        {
            return await _context.NguoiDungs
                .Include(nd => nd.Khoa)
                .AsNoTracking()
                .ToListAsync();
        }
        
    }
}