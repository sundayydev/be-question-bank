using BeQuestionBank.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Infrastructure.Persistence;

public class EfDbTransaction(IDbContextTransaction transaction) : IDbTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
    public Task RollbackAsync(CancellationToken cancellationToken = default) => transaction.RollbackAsync(cancellationToken);
    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}
