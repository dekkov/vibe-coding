namespace Backend.DTOs;

public sealed class CardView
{
    public string? Rank { get; set; }  // "Ace", "King", "Queen", "Jack", null for Joker
    public string? Suit { get; set; }  // "Spades", "Hearts", "Diamonds", "Clubs", null for Joker
    public bool IsJoker { get; set; }

    public static CardView FromCard(Backend.Models.Card card)
    {
        if (card.IsJoker)
        {
            return new CardView { IsJoker = true };
        }

        return new CardView
        {
            Rank = card.Rank!.Value.ToString(),
            Suit = card.Suit!.Value.ToString(),
            IsJoker = false
        };
    }
}

public sealed class PlayerView
{
    public int Index { get; set; }
    public int Chips { get; set; }
    public bool HasFolded { get; set; }
    public List<CardView> Hand { get; set; } = new();
    public int CommittedThisStreet { get; set; }
}

public sealed class BettingView
{
    public int StreetIndex { get; set; }  // 0 = pre-draw, 1 = post-draw
    public int CurrentBet { get; set; }
    public int Increment { get; set; } = 5;
    public int Cap { get; set; } = 30;
    public int ToActPlayerIndex { get; set; }
    public bool IsClosed { get; set; }
}

public sealed class ShowdownView
{
    public int WinnerIndex { get; set; }
    public List<HandResultView> Hands { get; set; } = new();
}

public sealed class HandResultView
{
    public string HandType { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> PrimaryRanks { get; set; } = new();
    public List<string> Kickers { get; set; } = new();
}

public sealed class ActionCapabilities
{
    public bool CanCheck { get; set; }
    public bool CanBet { get; set; }
    public bool CanCall { get; set; }
    public bool CanRaise { get; set; }
    public bool CanFold { get; set; }
    public bool CanDiscard { get; set; }
    public bool CanNextHand { get; set; }
}

public sealed class GameView
{
    public string GameId { get; set; } = "";
    public string Phase { get; set; } = "";
    public int HandNumber { get; set; }
    public int StartingPlayerIndex { get; set; }
    public int Pot { get; set; }
    public int DeckRemaining { get; set; }
    public List<PlayerView> Players { get; set; } = new();
    public BettingView? Betting { get; set; }
    public ShowdownView? Showdown { get; set; }
    public ActionCapabilities ActionCapabilities { get; set; } = new();
    public string LastEvent { get; set; } = "";
    public bool IsMatchComplete { get; set; }
    public int? MatchWinnerIndex { get; set; }
    public int? DrawPhaseActivePlayer { get; set; } // Whose turn it is in draw phase
}
