namespace BeQuestionBank.Test.Domain;

using System.Collections.Generic;
using BeQuestionBank.Domain.Exceptions;
using Xunit;

public class DomainExceptionTests
{
    [Fact]
    public void NotFoundException_ShouldSetCorrectMessage()
    {
        var ex = new NotFoundException("CauHoi", 123);
        Assert.Equal("Entity \"CauHoi\" (123) was not found.", ex.Message);
    }

    [Fact]
    public void ValidationException_ShouldStoreErrors()
    {
        var errors = new Dictionary<string, string[]> { { "TenKhoa", new[] { "Required" } } };
        var ex = new ValidationException(errors);
        Assert.True(ex.Errors.ContainsKey("TenKhoa"));
    }
}
