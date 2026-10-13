namespace CardAccess;

public static class AccessPipeline
{
    public static readonly Func<EnrichedRequest, bool> StrictPolicy = 
        req => req.Card.active && req.User.level == req.Room.level;

    public static readonly Func<EnrichedRequest, bool> HierarchicalPolicy = 
        req => req.Card.active && req.User.level >= req.Room.level;
    
    public static IEnumerable<EnrichedRequest> EnrichRequests(
        IEnumerable<AccessRequest> requests, 
        IReadOnlyList<User> users, 
        IReadOnlyList<Card> cards, 
        IReadOnlyList<Room> rooms)
    {
        foreach (var req in requests)
        {
            var user = users.FirstOrDefault(u => u.id == req.UserId);
            var card = cards.FirstOrDefault(c => c.id == req.CardId && c.userId == req.UserId);
            var room = rooms.FirstOrDefault(r => r.number == req.RoomNumber);
            if (user != null && card != null && room != null)
            {
                yield return new EnrichedRequest(user, card, room);
            }
        }
    }
    
    public static IEnumerable<(EnrichedRequest Request, bool Granted)> EvaluateAccess(
        IEnumerable<EnrichedRequest> validRequests, 
        Func<EnrichedRequest, bool> accessPolicy)
    {
        return validRequests.Select(req => (req, accessPolicy(req)));
    }
    
    public static IEnumerable<AccessResult> FormatResults(
        IEnumerable<(EnrichedRequest Request, bool Granted)> evaluated)
    {
        return evaluated.Select(e => new AccessResult(
            UserName: $"{e.Request.User.name} {e.Request.User.surename}",
            RoomNumber: e.Request.Room.number,
            AccessGranted: e.Granted,
            Reason: e.Granted ? "Access Granted" : "Access Denied by Policy"
        ));
    }
    
    public static IEnumerable<AccessResult> ProcessWorkflow(
        IEnumerable<AccessRequest> requests,
        IReadOnlyList<User> users,
        IReadOnlyList<Card> cards,
        IReadOnlyList<Room> rooms,
        Func<EnrichedRequest, bool> accessPolicy)
    {
        var stage1 = EnrichRequests(requests, users, cards, rooms);
        var stage2 = EvaluateAccess(stage1, accessPolicy);
        return FormatResults(stage2);
    }
}