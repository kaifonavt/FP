using System.Collections.Generic;
using System.Linq;
using Xunit;
using CardAccess;

namespace CardAccess.Tests;

public class AccessPipelineTests
{
    private readonly User _adminUser = new(1, "Alice", "Smith", AccessLevel.Admin);
    private readonly User _staffUser = new(2, "Bob", "Jones", AccessLevel.Staff);
    private readonly User _guestUser = new(3, "Charlie", "Brown", AccessLevel.Guest);

    private readonly Room _guestRoom = new(101, AccessLevel.Guest);
    private readonly Room _staffRoom = new(202, AccessLevel.Staff);
    private readonly Room _adminRoom = new(303, AccessLevel.Admin);

    [Fact]
    public void EnrichRequests_ShouldMatchValidUserCardAndRoom()
    {
        var activeCard = new Card("card1", 1, true);
        var requests = new List<AccessRequest> { new(1, "card1", 101) };

        var enriched = AccessPipeline.EnrichRequests(
            requests,
            new[] { _adminUser },
            new[] { activeCard },
            new[] { _guestRoom }
        ).ToList();

        Assert.Single(enriched);
        Assert.Equal(_adminUser.id, enriched[0].User.id);
        Assert.Equal(activeCard.id, enriched[0].Card.id);
        Assert.Equal(_guestRoom.number, enriched[0].Room.number);
    }

    [Fact]
    public void StrictPolicy_ShouldGrantAccessOnlyWhenLevelsMatchExactly()
    {
        var activeCard = new Card("card1", 1, true);
        var enrichedMatching = new EnrichedRequest(_adminUser, activeCard, _adminRoom);
        var enrichedMismatched = new EnrichedRequest(_adminUser, activeCard, _staffRoom);

        Assert.True(AccessPipeline.StrictPolicy(enrichedMatching));
        Assert.False(AccessPipeline.StrictPolicy(enrichedMismatched));
    }

    [Fact]
    public void HierarchicalPolicy_ShouldGrantAccessWhenUserLevelIsHigherOrEqual()
    {
        var activeCard = new Card("card1", 1, true);
        var enrichedAdminToStaff = new EnrichedRequest(_adminUser, activeCard, _staffRoom);
        var enrichedStaffToAdmin = new EnrichedRequest(_staffUser, activeCard, _adminRoom);

        Assert.True(AccessPipeline.HierarchicalPolicy(enrichedAdminToStaff));
        Assert.False(AccessPipeline.HierarchicalPolicy(enrichedStaffToAdmin));
    }

    [Fact]
    public void Pipeline_ShouldDenyAccessIfCardIsInactive()
    {
        var inactiveCard = new Card("card2", 1, false);
        var enriched = new EnrichedRequest(_adminUser, inactiveCard, _adminRoom);

        Assert.False(AccessPipeline.StrictPolicy(enriched));
        Assert.False(AccessPipeline.HierarchicalPolicy(enriched));
    }

    [Fact]
    public void ProcessWorkflow_ShouldExecuteFullPipelineAndFormatResults()
    {
        var card = new Card("card1", 1, true);
        var requests = new List<AccessRequest> { new(1, "card1", 303) };

        var results = AccessPipeline.ProcessWorkflow(
            requests,
            new[] { _adminUser },
            new[] { card },
            new[] { _adminRoom },
            AccessPipeline.StrictPolicy
        ).ToList();

        Assert.Single(results);
        Assert.True(results[0].AccessGranted);
        Assert.Equal("Alice Smith", results[0].UserName);
        Assert.Equal(303, results[0].RoomNumber);
    }
}