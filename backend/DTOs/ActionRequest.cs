namespace Backend.DTOs;

public sealed class ActionRequest
{
    public string GameId { get; set; } = "";
    public int PlayerIndex { get; set; }
    public string ActionType { get; set; } = "";  // "Check", "Bet", "Call", "Raise", "Fold", "Discard", "NextHand"
    public int Amount { get; set; }  // For Bet/Raise
    public List<int> CardIndices { get; set; } = new();  // For Discard (0-4)
}

public sealed class NewGameRequest
{
    // For future expansion - could add player names, starting chips, etc.
}

public sealed class GameResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public GameView? Game { get; set; }
}
