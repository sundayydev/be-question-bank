namespace BeQuestionBank.Application.Common.Interfaces;

using System.Threading;
using System.Threading.Tasks;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbTransaction> BeginTransactionAsync();
}
