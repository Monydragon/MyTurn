namespace MyTurn.Application;

public sealed record SaveSlotSummary(Guid Id, string Name, DateTime CreatedAtUtc, DateTime LastPlayedAtUtc);
