namespace BeQuestionBank.Application.Common.Interfaces.Services;

using System;
using System.IO;
using System.Threading.Tasks;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream stream, string fileName, string folder);
    Task<byte[]?> GetFileAsync(string relativePath);
    Task<bool> DeleteFileAsync(string relativePath);
}
