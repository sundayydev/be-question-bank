using BeQuestionBank.Domain.Interfaces.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BEQuestionBank.Core.Services;

public class FileService(IFileRepository repository)
{
    private readonly IFileRepository _repository = repository;

}
