namespace MyTurn.Application;

public sealed record WorldGenerationRequest(
    int? Seed = null,
    int Size = 15);
