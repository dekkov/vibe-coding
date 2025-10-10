namespace Backend.Models;

public enum Phase
{
    NewHand,
    PreDrawBetting,
    Draw,
    PostDrawBetting,
    Showdown,
    HandComplete,
    MatchComplete
}

public enum ActionType
{
    Check,
    Bet,
    Call,
    Raise,
    Fold,
    Discard,
    NextHand
}

public sealed class PlayerAction
{
    public ActionType Type { get; }
    public int Amount { get; } // For Bet/Raise
    public List<int> CardIndices { get; } // For Discard (0-4 indices)

    private PlayerAction(ActionType type, int amount = 0, List<int>? cardIndices = null)
    {
        Type = type;
        Amount = amount;
        CardIndices = cardIndices ?? new List<int>();
    }

    public static PlayerAction Check() => new(ActionType.Check);
    public static PlayerAction Bet(int amount) => new(ActionType.Bet, amount);
    public static PlayerAction Call() => new(ActionType.Call);
    public static PlayerAction Raise(int amount) => new(ActionType.Raise, amount);
    public static PlayerAction Fold() => new(ActionType.Fold);
    public static PlayerAction Discard(List<int> cardIndices) => new(ActionType.Discard, cardIndices: cardIndices);
    public static PlayerAction NextHand() => new(ActionType.NextHand);

    public override string ToString() => Type switch
    {
        ActionType.Check => "Check",
        ActionType.Bet => $"Bet {Amount}",
        ActionType.Call => "Call",
        ActionType.Raise => $"Raise to {Amount}",
        ActionType.Fold => "Fold",
        ActionType.Discard => $"Discard {CardIndices.Count} cards",
        ActionType.NextHand => "Next Hand",
        _ => Type.ToString()
    };
}

public sealed class PlayerState
{
    public int PlayerIndex { get; }
    public List<Card> Hand { get; private set; }
    public int Chips { get; private set; }
    public int CommittedThisStreet { get; private set; }
    public bool HasFolded { get; private set; }

    public PlayerState(int playerIndex, int startingChips = 100)
    {
        PlayerIndex = playerIndex;
        Hand = new List<Card>();
        Chips = startingChips;
        CommittedThisStreet = 0;
        HasFolded = false;
    }

    public void DealCard(Card card) => Hand.Add(card);
    
    public void ClearHand() => Hand.Clear();
    
    public int CommitChips(int amount)
    {
        // All-in logic: commit all remaining chips if amount exceeds what player has
        var actualAmount = Math.Min(amount, Chips);
        Chips -= actualAmount;
        CommittedThisStreet += actualAmount;
        return actualAmount; // Return actual amount committed for all-in tracking
    }

    public void WinChips(int amount) => Chips += amount;
    
    public void ResetStreet() => CommittedThisStreet = 0;
    
    public void Fold() => HasFolded = true;
    
    public void ResetForNewHand()
    {
        ClearHand();
        CommittedThisStreet = 0;
        HasFolded = false;
    }

    public void DiscardCards(List<int> indices)
    {
        if (indices.Any(i => i < 0 || i >= Hand.Count))
            throw new ArgumentException("Invalid card index for discard");

        // Remove cards in reverse order to maintain indices
        foreach (var index in indices.OrderByDescending(i => i))
        {
            Hand.RemoveAt(index);
        }
    }

    public void DrawCards(List<Card> cards) => Hand.AddRange(cards);
}

public sealed class BettingState
{
    public int StreetIndex { get; private set; } // 0 = pre-draw, 1 = post-draw
    public int CurrentBet { get; private set; }
    public int ToActPlayerIndex { get; private set; }
    public bool IsClosed { get; private set; }
    public int LastRaiseAmount { get; private set; }

    public const int Increment = 5;
    public const int Cap = 30;

    public BettingState(int startingPlayerIndex)
    {
        StreetIndex = 0;
        CurrentBet = 0;
        ToActPlayerIndex = startingPlayerIndex;
        IsClosed = false;
        LastRaiseAmount = 0;
        StartingPlayerIndex = startingPlayerIndex;
    }

    public void NextStreet(int startingPlayerIndex)
    {
        StreetIndex++;
        CurrentBet = 0;
        ToActPlayerIndex = startingPlayerIndex;
        IsClosed = false;
        LastRaiseAmount = 0;
        StartingPlayerIndex = startingPlayerIndex;
    }

    public int StartingPlayerIndex { get; private set; }

    public void ProcessAction(PlayerAction action, PlayerState[] players)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot act on closed betting round");

        var actingPlayer = players[ToActPlayerIndex];
        var otherPlayer = players[1 - ToActPlayerIndex];

        switch (action.Type)
        {
            case ActionType.Check:
                if (CurrentBet > 0)
                    throw new InvalidOperationException("Cannot check when there is a bet to call");
                
                // If this is the starting player checking first, just switch turns
                if (ToActPlayerIndex == StartingPlayerIndex && actingPlayer.CommittedThisStreet == 0 && otherPlayer.CommittedThisStreet == 0)
                {
                    ToActPlayerIndex = 1 - ToActPlayerIndex;
                }
                // If the other player has also checked (0 committed), close the street
                else if (otherPlayer.CommittedThisStreet == 0)
                {
                    IsClosed = true;
                }
                // Otherwise, just switch turns
                else
                {
                    ToActPlayerIndex = 1 - ToActPlayerIndex;
                }
                break;

            case ActionType.Bet:
                if (CurrentBet > 0)
                    throw new InvalidOperationException("Cannot bet when there is already a bet");
                if (action.Amount < 1)
                    throw new InvalidOperationException("Bet amount must be at least 1");
                if (action.Amount > Cap)
                    throw new InvalidOperationException($"Bet amount cannot exceed cap of {Cap}");
                
                var actualBetAmount = actingPlayer.CommitChips(action.Amount);
                CurrentBet = actualBetAmount;
                LastRaiseAmount = actualBetAmount;
                ToActPlayerIndex = 1 - ToActPlayerIndex;
                break;

            case ActionType.Call:
                if (CurrentBet == 0)
                    throw new InvalidOperationException("Nothing to call");
                
                var toCall = CurrentBet - actingPlayer.CommittedThisStreet;
                actingPlayer.CommitChips(toCall);
                IsClosed = true;
                break;

            case ActionType.Raise:
                if (CurrentBet == 0)
                    throw new InvalidOperationException("Cannot raise without a bet");
                if (action.Amount <= CurrentBet)
                    throw new InvalidOperationException("Raise amount must be higher than current bet");
                if (action.Amount > Cap)
                    throw new InvalidOperationException($"Raise amount cannot exceed cap of {Cap}");
                
                var raiseAmount = action.Amount - actingPlayer.CommittedThisStreet;
                var actualRaiseAmount = actingPlayer.CommitChips(raiseAmount);
                CurrentBet = actingPlayer.CommittedThisStreet;
                LastRaiseAmount = actualRaiseAmount;
                ToActPlayerIndex = 1 - ToActPlayerIndex;
                break;

            case ActionType.Fold:
                actingPlayer.Fold();
                IsClosed = true;
                break;

            default:
                throw new InvalidOperationException($"Invalid betting action: {action.Type}");
        }
    }

    public bool CanCheck(PlayerState player) => CurrentBet == 0;
    public bool CanBet(PlayerState player) => CurrentBet == 0 && player.Chips > 0;
    public bool CanCall(PlayerState player) => CurrentBet > 0 && player.Chips > 0;
    public bool CanRaise(PlayerState player) => CurrentBet > 0 && CurrentBet < Cap && player.Chips > 0;
}

public sealed class ActionLogEntry
{
    public int HandNumber { get; }
    public int PlayerIndex { get; }
    public PlayerAction Action { get; }
    public string Description { get; }
    public DateTime Timestamp { get; }

    public ActionLogEntry(int handNumber, int playerIndex, PlayerAction action, string description)
    {
        HandNumber = handNumber;
        PlayerIndex = playerIndex;
        Action = action;
        Description = description;
        Timestamp = DateTime.UtcNow;
    }
}
