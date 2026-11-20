namespace BeQuestionBank.Application.Common.Interfaces.Services;

using System;
using System.IO;
using System.Threading.Tasks;

public interface IExportService
{
    Task<byte[]> ExportDeThiToWordAsync(Guid maDeThi);
    Task<byte[]> ExportDeThiToPdfAsync(Guid maDeThi);
}
