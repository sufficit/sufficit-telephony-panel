namespace Sufficit.Telephony.Panel.Operations;

public sealed record QueueBoardQueue(string QueueId, string Title, int Waiting, int Capacity,
    DateTimeOffset? OldestEnteredAt, int Offering = 0, int Connected = 0, int Greeting = 0, int Announcing = 0);
