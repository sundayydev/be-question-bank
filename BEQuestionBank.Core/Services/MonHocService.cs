using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using BeQuestionBank.Shared.DTOs.MonHoc;
using BeQuestionBank.Shared.DTOs.Pagination;

namespace BEQuestionBank.Core.Services;

public class MonHocService(IMonHocRepository repository)
{
    private readonly IMonHocRepository _repository = repository;

    public async Task<IEnumerable<MonHoc?>> GetMonHocsByMaKhoaAsync(Guid maKhoa)
    {
        return await _repository.GetByMaKhoaAsync(maKhoa);
    }

    public async Task<MonHoc?> GetMonHocByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<MonHoc?>> GetAllMonHocsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task AddMonHocAsync(MonHoc monHoc)
    {
        await _repository.AddAsync(monHoc);
    }

    public async Task UpdateMonHocAsync(MonHoc monHoc)
    {
        await _repository.UpdateAsync(monHoc);
    }

    public async Task DeleteMonHocAsync(MonHoc monHoc)
    {
        await _repository.DeleteAsync(monHoc);
    }

    public async Task<IEnumerable<MonHoc?>> FindMonHocsAsync(Expression<Func<MonHoc, bool>> predicate)
    {
        return await _repository.FindAsync(predicate);
    }

    public async Task<MonHoc?> FirstOrDefaultMonHocAsync(Expression<Func<MonHoc, bool>> predicate)
    {
        return await _repository.FirstOrDefaultAsync(predicate);
    }
    public async Task<PagedResult<MonHocDto>> GetTrashedAsync(int page = 1, int pageSize = 10)
    {
        return await _repository.GetTrashedAsync(page, pageSize);
    }
}
