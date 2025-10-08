
using System.Threading.Tasks;
using FEQuestionBank.Client.Share; 

namespace FEQuestionBank.Client.Services
{
    public interface IKhoaApiClient
    {
        Task<List<KhoaDto>> GetKhoasAsync();
    }
}