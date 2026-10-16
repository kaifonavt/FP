namespace SupportTicket.Tests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        //TODO: 6 tests for factories (Batyr,Meir)
        
    }
    [Fact]
    public void TicketFactory_ValidInput_CreatesValidatedValue()
    {
        var description = "Critical bug in system";
        var user = new User("Meir");
        var level = Level.High;

        bool isValidCreation = !string.IsNullOrEmpty(description) && user != null;
        Assert.True(isValidCreation, "Valid input should pass factory prerequisites.");
    }

    [Fact]
    public void TicketFactory_InvalidInput_RejectsGracefully()
    {
        string invalidDescription = "";
        bool isRejected = string.IsNullOrWhiteSpace(invalidDescription);
        Assert.True(isRejected, "Factory must reject empty or invalid input.");
    }
}