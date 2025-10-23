namespace Backend.Models;

public enum GameStatus
{
    Waiting,      // Waiting for players to join
    InProgress,   // Game is active
    Complete      // Game finished
}

public sealed class PlayerConnection
{
    public string Username { get; set; } = "";
    public string ConnectionId { get; set; } = "";
    public int PlayerIndex { get; set; } = -1; // -1 means not assigned yet
    public bool IsReady { get; set; } = false;
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
}

public sealed class GameRoom
{
    public string RoomId { get; set; } = "";
    public GameState? GameState { get; set; }
    public Dictionary<string, PlayerConnection> Players { get; set; } = new();
    public GameStatus Status { get; set; } = GameStatus.Waiting;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    public int MaxPlayers { get; set; } = 2;
    
    public int PlayerCount => Players.Count;
    public bool IsFull => PlayerCount >= MaxPlayers;
    public bool IsEmpty => PlayerCount == 0;
    
    public bool CanStart => PlayerCount == MaxPlayers && 
                           Players.Values.All(p => p.IsReady) && 
                           Status == GameStatus.Waiting;
}

public sealed class RoomInfo
{
    public string RoomId { get; set; } = "";
    public int PlayerCount { get; set; }
    public int MaxPlayers { get; set; }
    public GameStatus Status { get; set; }
    public int HandNumber { get; set; }
    public int Pot { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> PlayerNames { get; set; } = new();
}

public sealed class MatchResult
{
    public int WinnerIndex { get; set; }
    public string WinnerUsername { get; set; } = "";
    public int WinnerChips { get; set; }
    public int FinalPot { get; set; }
    public int TotalHands { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
