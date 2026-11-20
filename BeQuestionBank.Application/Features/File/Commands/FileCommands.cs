using BeQuestionBank.Application.Common.Interfaces.Services;
using BeQuestionBank.Domain.Enums;
using BeQuestionBank.Shared.DTOs.File;
using BeQuestionBank.Shared.DTOs.Pagination;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.File.Commands;

public record DeleteFileCommand(Guid MaFile) : IRequest<bool>;

public class DeleteFileCommandHandler(IFileStorageService fileStorageService) : IRequestHandler<DeleteFileCommand, bool>
{
    private readonly IFileStorageService _fileStorageService = fileStorageService;

    public Task<bool> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        return _fileStorageService.DeleteFileAsync(request.MaFile);
    }
}
