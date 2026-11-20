namespace BeQuestionBank.Test.Features;

using BE_CIRRO.Shared.DTOs.Auth;
using BeQuestionBank.Application.Common.Interfaces.Services;
using BeQuestionBank.Application.Features.Auth.Commands;
using BeQuestionBank.Application.Features.Khoa.Commands;
using BeQuestionBank.Domain.Enums;
using BeQuestionBank.Domain.Exceptions;
using BeQuestionBank.Domain.Interfaces.IRepositories;
using BEQuestionBank.Domain.Interfaces.Repo;
using BeQuestionBank.Domain.Models;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class AuthAndKhoaHandlerTests
{
    [Fact]
    public async Task CreateKhoaCommandHandler_ShouldCallRepositoryAdd()
    {
        var repoMock = new Mock<IKhoaRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<Khoa>())).Returns(Task.CompletedTask);

        var handler = new CreateKhoaCommandHandler(repoMock.Object);
        var command = new CreateKhoaCommand { TenKhoa = "Cong Nghe Thong Tin", MaKhoa = "CNTT" };

        var result = await handler.Handle(command, CancellationToken.None);
        Assert.NotNull(result);
        repoMock.Verify(r => r.AddAsync(It.IsAny<Khoa>()), Times.Once);
    }

    [Fact]
    public async Task LoginCommandHandler_ShouldThrowValidation_WhenUserNotFound()
    {
        var userRepoMock = new Mock<INguoiDungRepository>();
        userRepoMock.Setup(r => r.GetByUsernameAsync("nonexistent")).ReturnsAsync((NguoiDung)null!);

        var jwtMock = new Mock<IJwtService>();
        var redisMock = new Mock<IRedisCacheService>();

        var handler = new LoginCommandHandler(userRepoMock.Object, jwtMock.Object, redisMock.Object);
        var command = new LoginCommand { TenDangNhap = "nonexistent", MatKhau = "password" };

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }
}
