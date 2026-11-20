using BeQuestionBank.Application.Common.Interfaces.Services;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace BeQuestionBank.Application.Features.Tool.Commands;

public record ConvertLatexCommand(string Content) : IRequest<string>;

public class ConvertLatexCommandHandler(IToolService toolService) : IRequestHandler<ConvertLatexCommand, string>
{
    private readonly IToolService _toolService = toolService;

    public Task<string> Handle(ConvertLatexCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_toolService.ProcessContentWithLatexAndMergeSpans(request.Content));
    }
}

public record UploadImageToolCommand(byte[] Bytes, string ContentType) : IRequest<string>;

public class UploadImageToolCommandHandler(IToolService toolService) : IRequestHandler<UploadImageToolCommand, string>
{
    private readonly IToolService _toolService = toolService;

    public Task<string> Handle(UploadImageToolCommand request, CancellationToken cancellationToken)
    {
        var base64 = _toolService.ConvertToBase64(request.Bytes, request.ContentType);
        var html = _toolService.BuildImageHtml(base64);
        return Task.FromResult(html);
    }
}
