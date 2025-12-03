using BeQuestionBank.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.Khoa;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace BeQuestionBank.Domain.Interfaces.IRepositories;

public interface IKhoaRepository : IRepository<Khoa>
{
    Task<Khoa?> GetByTenKhoaAsync(string tenKhoa);
    Task<PagedResult<KhoaDto>> GetTrashedAsync(int page = 1, int pageSize = 10);
}
