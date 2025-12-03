using BeQuestionBank.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace BeQuestionBank.Domain.Interfaces.IRepositories;

public interface IMonHocRepository : IRepository<MonHoc>
{
    Task<IEnumerable<MonHoc?>> GetByMaKhoaAsync(Guid maKhoa);
    Task<PagedResult<MonHocDto>> GetTrashedAsync(int page = 1, int pageSize = 10);
}
