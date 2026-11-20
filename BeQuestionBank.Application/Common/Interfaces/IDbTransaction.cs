namespace BeQuestionBank.Application.Common.Interfaces;

using System;
using System.Threading;
using System.Threading.Tasks;

public interface IDbTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
