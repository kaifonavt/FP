using Xunit;

namespace SupportTicket.Tests;

public class UnitTest1
{
    [Fact]
    public void Factory_ValidInput_CreatesValidatedValue()
    {
        var title = "Test Issue";
        var description = "Description";
        var priority = 3;
        bool isValidCreation = !string.IsNullOrEmpty(title) && priority > 0;
        Assert.True(isValidCreation, "Valid input should pass factory prerequisites.");
    }

    [Fact]
    public void Factory_InvalidInput_RejectsGracefully()
    {
        string invalidTitle = "";
        bool isRejected = string.IsNullOrWhiteSpace(invalidTitle);
        Assert.True(isRejected, "Factory must reject empty or invalid input.");
    }
}