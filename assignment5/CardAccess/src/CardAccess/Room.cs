namespace CardAccess;

using System;

public enum RoomType
{
    Public,
    Staff,
    Admin
}

public sealed record Room
{
    public int Number { get; }
    public RoomType Type { get; }

    public Room(int number, RoomType roomType)
    {
        if (number <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(number),
                "Room number must be greater than zero.");

        if (!Enum.IsDefined(typeof(RoomType), roomType))
            throw new ArgumentOutOfRangeException(
                nameof(roomType),
                "Unknown room type.");

        Number = number;
        Type = roomType;
    }
}