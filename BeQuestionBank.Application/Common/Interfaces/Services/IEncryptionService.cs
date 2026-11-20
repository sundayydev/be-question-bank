namespace BeQuestionBank.Application.Common.Interfaces.Services;

using System.IO;
using System.Threading.Tasks;

public interface IEncryptionService
{
    byte[] EncryptEzp(byte[] data, string password);
    byte[] DecryptEzp(byte[] data, string password);
}
