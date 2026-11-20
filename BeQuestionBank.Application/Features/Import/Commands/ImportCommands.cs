using BeQuestionBank.Application.Common.Interfaces.Services;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.Import.Commands;

public record ImportWordCommand(IFormFile File, Guid MaPhan, string? MediaFolderPath = null) : IRequest<object>;

public class ImportWordCommandHandler(IImportService importService) : IRequestHandler<ImportWordCommand, object>
{
    private readonly IImportService _importService = importService;

    public Task<object> Handle(ImportWordCommand request, CancellationToken cancellationToken)
    {
        return _importService.ImportQuestionsAsync(request.File, request.MaPhan, request.MediaFolderPath);
    }
}

public record PreviewWordCommand(IFormFile File, string? MediaFolderPath = null) : IRequest<object>;

public class PreviewWordCommandHandler(IImportService importService) : IRequestHandler<PreviewWordCommand, object>
{
    private readonly IImportService _importService = importService;

    public Task<object> Handle(PreviewWordCommand request, CancellationToken cancellationToken)
    {
        return _importService.PreviewImportAsync(request.File, request.MediaFolderPath);
    }
}
