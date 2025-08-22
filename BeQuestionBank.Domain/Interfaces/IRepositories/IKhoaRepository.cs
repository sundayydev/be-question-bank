using BeQuestionBank.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeQuestionBank.Domain.Interfaces.IRepositories;

public interface IKhoaRepository : IRepository<Khoa>
{
    Task<Khoa?> GetByTenKhoaAsync(string tenKhoa);
}
