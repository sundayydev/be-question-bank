using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.DeThi;
using BEQuestionBank.Shared.DTOs.DeThi;

namespace BeQuestionBank.Domain.Interfaces.IRepositories;

public interface IDeThiRepository :  IRepository<DeThi>
{
    Task<Object> GetByIdWithChiTietAsync(Guid id);
    Task<IEnumerable<Object>> GetAllWithChiTietAsync();
    Task<IEnumerable<Object>> GetAllBasicAsync();
    Task<Object> UpdateWithChiTietAsync(DeThiDto deThiDto);
    Task<IEnumerable<DeThi>> GetByMaMonHocAsync(Guid maMonHoc);
    Task<IEnumerable<Object>> GetApprovedDeThisAsync();
    Task<Object> GetDeThiWithChiTietAndCauTraLoiAsync(Guid maDeThi);
    Task<DeThi?> GetFullForExportAsync(Guid maDeThi);

}