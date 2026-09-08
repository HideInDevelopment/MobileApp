using Anthropometry.Domain.Common;

namespace Anthropometry.Domain.Tests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_contains_value_and_no_error()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_contains_error_and_no_value()
    {
        var error = new DomainError("sample.invalid", "Errors.SampleInvalid");
        var result = Result.Failure<int>(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }
}
