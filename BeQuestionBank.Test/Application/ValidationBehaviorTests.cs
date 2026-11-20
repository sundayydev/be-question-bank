namespace BeQuestionBank.Test.Application;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeQuestionBank.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Xunit;

public class ValidationBehaviorTests
{
    public record TestCommand(string Name) : IRequest<string>;

    public class TestValidator : AbstractValidator<TestCommand>
    {
        public TestValidator() => RuleFor(x => x.Name).NotEmpty();
    }

    [Fact]
    public async Task ValidationBehavior_ShouldThrow_WhenValidationFails()
    {
        var validators = new List<IValidator<TestCommand>> { new TestValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("");

        await Assert.ThrowsAsync<BeQuestionBank.Domain.Exceptions.ValidationException>(() =>
            behavior.Handle(command, () => Task.FromResult("ok"), CancellationToken.None));
    }

    [Fact]
    public async Task ValidationBehavior_ShouldProceed_WhenValidationSucceeds()
    {
        var validators = new List<IValidator<TestCommand>> { new TestValidator() };
        var behavior = new ValidationBehavior<TestCommand, string>(validators);
        var command = new TestCommand("ValidName");

        var result = await behavior.Handle(command, () => Task.FromResult("ok"), CancellationToken.None);
        Assert.Equal("ok", result);
    }
}
