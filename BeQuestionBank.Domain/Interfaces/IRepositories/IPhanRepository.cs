using BeQuestionBank.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;
using BeQuestionBank.Shared.DTOs.Phan;

namespace BeQuestionBank.Domain.Interfaces.IRepositories;

public interface IPhanRepository : IRepository<Phan>
{
    Task<IEnumerable<Phan?>> GetByMaMonHocAsync(Guid maMonHoc);
    Task<List<Phan>> GetSiblingsAsync(Guid maMonHoc, Guid? maPhanCha);
    Task<PagedResult<PhanDto>> GetTrashedAsync(int page = 1, int pageSize = 10);
}
