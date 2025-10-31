
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.Enums;

namespace BEQuestionBank.Domain.Interfaces.Repo
{
    public interface INguoiDungRepository : IRepository<NguoiDung>
    {
        Task<NguoiDung> GetByIdAsync(Guid maNguoiDung);
        Task<NguoiDung> GetByUsernameAsync(string tenDangNhap);
        Task<IEnumerable<NguoiDung>> GetByVaiTroAsync(EnumRole vaiTro);
        Task<IEnumerable<NguoiDung>> GetByKhoaAsync(Guid maKhoa);
        Task<bool> IsLockedAsync(string tenDangNhap);
        Task<NguoiDung> GetByResetCodeAsync(Guid maKhoa);
    }
}