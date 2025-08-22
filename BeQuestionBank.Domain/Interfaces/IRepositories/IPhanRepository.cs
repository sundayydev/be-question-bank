using BeQuestionBank.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Domain.Interfaces.IRepositories;

public interface IPhanRepository : IRepository<Phan>
{
    Task<IEnumerable<Phan?>> GetByMaMonHocAsync(Guid maMonHoc);
    Task<List<Phan>> GetSiblingsAsync(Guid maMonHoc, Guid? maPhanCha);
}
