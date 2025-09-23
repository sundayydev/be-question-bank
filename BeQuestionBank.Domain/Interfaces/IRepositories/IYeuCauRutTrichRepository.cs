using BeQuestionBank.Domain.Models;

namespace BeQuestionBank.Domain.Interfaces.IRepositories;

public interface IYeuCauRutTrichRepository :  IRepository<YeuCauRutTrich>
{
    Task<IEnumerable<YeuCauRutTrich>> GetByMaNguoiDungAsync(Guid maNguoiDung);
    Task<IEnumerable<YeuCauRutTrich>> GetByMaMonHocAsync(Guid maMonHoc);
    
    Task<IEnumerable<YeuCauRutTrich>> GetByTrangThaiAsync(bool? daXuLy);
}