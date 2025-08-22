using BeQuestionBank.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Domain.Interfaces.IRepositories;

public interface IMonHocRepository : IRepository<MonHoc>
{
    Task<IEnumerable<MonHoc?>> GetByMaKhoaAsync(Guid maKhoa);
}
