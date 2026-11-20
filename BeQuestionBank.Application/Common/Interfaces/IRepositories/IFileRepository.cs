using BeQuestionBank.Domain.Models;
using BeQuestionBank.Shared.DTOs.File;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using File = BeQuestionBank.Domain.Models.File;

namespace BeQuestionBank.Domain.Interfaces.IRepositories;

public interface IFileRepository : IRepository<File>
{
    Task<IEnumerable<File?>> FindFilesByCauHoiAsync(Guid maCauHoi);
    Task<IEnumerable<File?>> FindFilesByCauTraLoiAsync(Guid maCauTraLoi);
}
