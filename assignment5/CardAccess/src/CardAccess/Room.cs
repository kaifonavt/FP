namespace CardAccess;
public sealed class Room
{
    public int number { get; }
    public AccessLevel level { get; }

    public Room(int number, AccessLevel level)
    {
        if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number), "Room number must be greater than zero.");
        if (!Enum.IsDefined(typeof(AccessLevel), level)) throw new ArgumentOutOfRangeException(nameof(level), "Unknown room type.");
        this.number = number;
        this.level = level;
    }
}