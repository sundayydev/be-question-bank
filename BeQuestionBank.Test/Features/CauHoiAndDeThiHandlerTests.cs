namespace BeQuestionBank.Test.Features;

using BeQuestionBank.Application.Features.CauHoi.Commands;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BeQuestionBank.Domain.Models;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class CauHoiAndDeThiHandlerTests
{
    [Fact]
    public async Task CreateCauHoiCommandHandler_ShouldPersistQuestion()
    {
        var repoMock = new Mock<ICauHoiRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<CauHoi>())).Returns(Task.CompletedTask);

        var handler = new CreateCauHoiCommandHandler(repoMock.Object);
        var command = new CreateCauHoiCommand { NoiDung = "Test question?", MaPhan = Guid.NewGuid() };

        var result = await handler.Handle(command, CancellationToken.None);
        Assert.NotNull(result);
        repoMock.Verify(r => r.AddAsync(It.IsAny<CauHoi>()), Times.Once);
    }
}
