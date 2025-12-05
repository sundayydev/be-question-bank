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
    // Đổi Object -> CauHoi
    Task<IEnumerable<CauHoi>> GetAllWithAnswersAsync();

    // Đổi Object -> CauHoi? (cho phép null)
    Task<CauHoi?> GetByIdWithAnswersAsync(Guid maCauHoi);

    // Lấy câu hỏi kèm theo các câu hỏi con (dành cho Group Questions)
    Task<CauHoi?> GetByIdWithChildrenAsync(Guid maCauHoi);

    Task<IEnumerable<CauHoi>> GetByCLoAsync(EnumCLO maCLo);
    Task<IEnumerable<CauHoi>> GetByMaPhanAsync(Guid maPhan);

    // Đổi Object -> CauHoi
    Task<IEnumerable<CauHoi>> GetByMaMonHocAsync(Guid maMonHoc);

    Task<IEnumerable<CauHoi>> GetByMaDeThiAsync(Guid maDeThi);
    Task<IEnumerable<CauHoi>> GetByMaCauHoiChasync(Guid maCHCha);

    // Đổi Object -> CauHoi
    Task<IEnumerable<CauHoi>> GetAllGroupsAsync();

    // Đổi tham số input và return type từ Object -> CauHoi
    Task<CauHoi> AddWithAnswersAsync(CauHoi cauHoi);
    Task<CauHoi> UpdateWithAnswersAsync(Guid maCauHoi, CauHoi cauHoi);

    Task<int> CountAsync(Expression<Func<CauHoi, bool>> predicate);
    Task AddRangeAsync(IEnumerable<CauHoi> cauHois);
    Task<int> GetMaxMaSoCauHoiAsync();
}   