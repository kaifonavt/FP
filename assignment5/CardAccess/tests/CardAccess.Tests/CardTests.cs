namespace CardAccess.Tests;

public class CardTests
{
    [Fact]
    public void GetHexId_ReturnsExpectedDeterministicHash()
    {
        int userId = 42;

        string hexId1 = Card.GetHexId(userId);
        string hexId2 = Card.GetHexId(userId);

        Assert.False(string.IsNullOrWhiteSpace(hexId1));
        Assert.Equal(16, hexId1.Length);
        Assert.Equal(hexId2, hexId1);
    }
}