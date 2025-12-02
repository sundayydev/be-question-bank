using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.Enums;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Domain.Interfaces.IRepositories;

public interface ICauHoiRepository : IRepository<CauHoi>
{
    Task<IEnumerable<Object>> GetAllWithAnswersAsync();
    Task<Object> GetByIdWithAnswersAsync(Guid maCauHoi);
    Task<IEnumerable<CauHoi>> GetByCLoAsync(EnumCLO maCLo);
    Task<IEnumerable<CauHoi>> GetByMaPhanAsync(Guid maPhan);
    Task<IEnumerable<Object>> GetByMaMonHocAsync(Guid maMonHoc);
    Task<IEnumerable<CauHoi>> GetByMaDeThiAsync(Guid maDeThi);
    Task<IEnumerable<CauHoi>> GetByMaCauHoiChasync(Guid maCHCha);
    Task<IEnumerable<Object>> GetAllGroupsAsync();
    Task<Object> AddWithAnswersAsync(Object cauHoiDto);
    Task<Object> UpdateWithAnswersAsync(Guid maCauHoi, Object cauHoiDto);
    Task<int> CountAsync(Expression<Func<CauHoi, bool>> predicate);
    Task AddRangeAsync(IEnumerable<CauHoi> cauHois);
    Task<int> GetMaxMaSoCauHoiAsync(); // Để tự tăng mã số câu hỏi
}
