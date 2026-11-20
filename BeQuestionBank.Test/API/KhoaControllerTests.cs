namespace BeQuestionBank.Test.API;

using BeQuestionBank.API.Controllers;
using BeQuestionBank.Application.Features.Khoa.Commands;
using BeQuestionBank.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using Xunit;

public class KhoaControllerTests
{
    [Fact]
    public async Task Create_ShouldSendCreateKhoaCommand()
    {
        var senderMock = new Mock<ISender>();
        senderMock.Setup(s => s.Send(It.IsAny<CreateKhoaCommand>(), default))
                  .ReturnsAsync(new Khoa { TenKhoa = "CNTT" });

        var controller = new KhoaController(senderMock.Object);
        var command = new CreateKhoaCommand { TenKhoa = "CNTT", MaKhoa = "CNTT" };

        var actionResult = await controller.CreateKhoaAsync(command);
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        senderMock.Verify(s => s.Send(command, default), Times.Once);
    }
}
