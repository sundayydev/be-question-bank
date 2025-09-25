using System.Linq.Expressions;
using BeQuestionBank.Core.Configurations;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace BEQuestionBank.Core.Repositories;

public class CauHoiRepository : GenericRepository<CauHoi>, ICauHoiRepository
{
    private readonly AppDbContext _context;

    public CauHoiRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }

    public Task<IEnumerable<object>> GetAllWithAnswersAsync()
    {
        throw new NotImplementedException();
    }

    public Task<object> GetByIdWithAnswersAsync(Guid maCauHoi)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<CauHoi>> GetByCLoAsync(EnumCLO maCLo)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<CauHoi>> GetByMaPhanAsync(Guid maPhan)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<object>> GetByMaMonHocAsync(Guid maMonHoc)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<CauHoi>> GetByMaDeThiAsync(Guid maDeThi)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<CauHoi>> GetByMaCauHoiChasync(Guid maCHCha)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<object>> GetAllGroupsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<object> AddWithAnswersAsync(object cauHoiDto)
    {
        throw new NotImplementedException();
    }

    public Task<object> UpdateWithAnswersAsync(Guid maCauHoi, object cauHoiDto)
    {
        throw new NotImplementedException();
    }

    public async Task<int> CountAsync(Expression<Func<CauHoi, bool>> predicate)
    {
        return await _context.CauHois. Include(c => c.Phan).CountAsync(predicate);
    }
}