namespace Backend.Models;

public sealed class GameState
{
    public string GameId { get; }
    public int HandNumber { get; private set; }
    public Deck Deck { get; private set; }
    public PlayerState[] Players { get; }
    public int Pot { get; private set; }
    public int StartingPlayerIndex { get; private set; }
    public BettingState? Betting { get; private set; }
    public Phase Phase { get; private set; }
    public List<ActionLogEntry> ActionLog { get; }
    public HandStrength[]? ShowdownResults { get; private set; }
    public int? WinnerIndex { get; private set; }

    private const int AnteAmount = 5;
    private const int MaxHands = 10;

    public GameState(string gameId)
    {
        GameId = gameId;
        HandNumber = 0;
        Deck = new Deck();
        Players = [new PlayerState(0), new PlayerState(1)];
        Pot = 0;
        StartingPlayerIndex = 0;
        Phase = Phase.NewHand;
        ActionLog = new List<ActionLogEntry>();
    }

    public void StartNewHand()
    {
        if (Phase != Phase.NewHand && Phase != Phase.HandComplete)
            throw new InvalidOperationException($"Cannot start new hand in phase {Phase}");

        if (HandNumber >= MaxHands)
        {
            Phase = Phase.MatchComplete;
            return;
        }

        HandNumber++;
        
        // Reset deck and players
        Deck.Reset();
        foreach (var player in Players)
        {
            player.ResetForNewHand();
        }

        // Collect antes
        CollectAntes();
        
        // If match ended during ante collection, don't continue
        if (Phase == Phase.MatchComplete)
            return;

        // Deal 5 cards to each player
        DealCards();

        // Set up betting
        Betting = new BettingState(StartingPlayerIndex);
        Phase = Phase.PreDrawBetting;

        LogAction(StartingPlayerIndex, PlayerAction.NextHand(), $"Hand {HandNumber} started. Player {StartingPlayerIndex + 1} to act first.");
    }

    public void ProcessAction(int playerIndex, PlayerAction action)
    {
        ValidateAction(playerIndex, action);

        switch (Phase)
        {
            case Phase.PreDrawBetting:
                ProcessBettingAction(playerIndex, action);
                if (Betting!.IsClosed)
                {
                    if (Players.Any(p => p.HasFolded))
                    {
                        ProcessFold();
                    }
                    else
                    {
                        StartDrawPhase();
                    }
                }
                break;

            case Phase.Draw:
                ProcessDrawAction(playerIndex, action);
                break;

            case Phase.PostDrawBetting:
                ProcessBettingAction(playerIndex, action);
                if (Betting!.IsClosed)
                {
                    if (Players.Any(p => p.HasFolded))
                    {
                        ProcessFold();
                    }
                    else
                    {
                        StartShowdown();
                    }
                }
                break;

            case Phase.HandComplete:
                if (action.Type == ActionType.NextHand)
                {
                    Phase = Phase.NewHand;
                    StartingPlayerIndex = 1 - StartingPlayerIndex; // Alternate starting player
                    StartNewHand();
                }
                break;

            default:
                throw new InvalidOperationException($"Cannot process action in phase {Phase}");
        }
    }

    private void ValidateAction(int playerIndex, PlayerAction action)
    {
        if (playerIndex < 0 || playerIndex >= Players.Length)
            throw new ArgumentException($"Invalid player index: {playerIndex}");

        if (Phase == Phase.PreDrawBetting || Phase == Phase.PostDrawBetting)
        {
            if (Betting!.ToActPlayerIndex != playerIndex)
                throw new InvalidOperationException($"It's not player {playerIndex}'s turn to act");
        }
        else if (Phase == Phase.Draw)
        {
            if (action.Type == ActionType.Discard)
            {
                // Validate discard count against deck remaining
                if (action.CardIndices.Count > Deck.CountRemaining)
                    throw new InvalidOperationException($"Cannot discard {action.CardIndices.Count} cards, only {Deck.CountRemaining} cards remaining in deck");

                // Check if it's the first player's turn (starting player goes first)
                var firstPlayerDone = ActionLog.Any(a => a.HandNumber == HandNumber && 
                                                   a.Action.Type == ActionType.Discard && 
                                                   a.PlayerIndex == StartingPlayerIndex);
                
                if (!firstPlayerDone)
                {
                    // First player (starting player) must go first
                    if (playerIndex != StartingPlayerIndex)
                        throw new InvalidOperationException($"Player {StartingPlayerIndex + 1} must discard first");
                }
                else
                {
                    // Second player's turn
                    var secondPlayerIndex = 1 - StartingPlayerIndex;
                    var secondPlayerDone = ActionLog.Any(a => a.HandNumber == HandNumber && 
                                                       a.Action.Type == ActionType.Discard && 
                                                       a.PlayerIndex == secondPlayerIndex);
                    
                    if (secondPlayerDone)
                        throw new InvalidOperationException("Both players have already discarded");
                        
                    if (playerIndex != secondPlayerIndex)
                        throw new InvalidOperationException($"It's player {secondPlayerIndex + 1}'s turn to discard");
                }
            }
        }
    }

    private void ProcessBettingAction(int playerIndex, PlayerAction action)
    {
        Betting!.ProcessAction(action, Players);
        
        // Move chips to pot when street closes
        if (Betting.IsClosed)
        {
            foreach (var player in Players)
            {
                Pot += player.CommittedThisStreet;
                player.ResetStreet();
            }
        }

        LogAction(playerIndex, action, $"Player {playerIndex + 1} {action}");
    }

    private void ProcessDrawAction(int playerIndex, PlayerAction action)
    {
        if (action.Type != ActionType.Discard)
            throw new InvalidOperationException("Only discard actions allowed in draw phase");

        var player = Players[playerIndex];
        var discardCount = action.CardIndices.Count;

        // Discard and draw
        player.DiscardCards(action.CardIndices);
        var newCards = Deck.Draw(discardCount);
        player.DrawCards(newCards);

        LogAction(playerIndex, action, $"Player {playerIndex + 1} discarded {discardCount} cards and drew {newCards.Count}");

        // Check if both players have drawn
        var bothPlayersDrew = ActionLog.Count(a => a.HandNumber == HandNumber && a.Action.Type == ActionType.Discard) == 2;
        if (bothPlayersDrew)
        {
            StartPostDrawBetting();
        }
    }

    private void ProcessFold()
    {
        var winner = Players.First(p => !p.HasFolded);
        winner.WinChips(Pot);
        WinnerIndex = winner.PlayerIndex;
        var wonAmount = Pot;
        Pot = 0;
        Phase = Phase.HandComplete;
        
        LogAction(WinnerIndex.Value, PlayerAction.NextHand(), $"Player {WinnerIndex + 1} wins {wonAmount} chips (opponent folded)");
        
        // Check if match should end due to someone running out of chips
        if (HandNumber >= MaxHands || Players.Any(p => p.Chips == 0))
        {
            Phase = Phase.MatchComplete;
            var matchWinner = Players[0].Chips > Players[1].Chips ? 0 : 1;
            
            if (Players.Any(p => p.Chips == 0))
            {
                LogAction(matchWinner, PlayerAction.NextHand(), 
                         $"Match complete! Player {matchWinner + 1} wins - opponent ran out of chips");
            }
            else
            {
                LogAction(matchWinner, PlayerAction.NextHand(), 
                         $"Match complete after 10 hands! Player {matchWinner + 1} wins with {Players[matchWinner].Chips} chips");
            }
        }
    }

    private void StartDrawPhase()
    {
        Phase = Phase.Draw;
        LogAction(-1, PlayerAction.NextHand(), "Draw phase started");
    }

    private void StartPostDrawBetting()
    {
        Betting!.NextStreet(StartingPlayerIndex);
        Phase = Phase.PostDrawBetting;
        LogAction(-1, PlayerAction.NextHand(), "Post-draw betting started");
    }

    private void StartShowdown()
    {
        Phase = Phase.Showdown;
        
        // Evaluate hands
        ShowdownResults = new HandStrength[2];
        ShowdownResults[0] = HandEvaluator.EvaluateHand(Players[0].Hand);
        ShowdownResults[1] = HandEvaluator.EvaluateHand(Players[1].Hand);

        // Determine winner
        var comparison = ShowdownResults[0].CompareTo(ShowdownResults[1]);
        WinnerIndex = comparison > 0 ? 0 : 1;

        // Award pot
        Players[WinnerIndex.Value].WinChips(Pot);
        LogAction(WinnerIndex.Value, PlayerAction.NextHand(), 
                 $"Player {WinnerIndex + 1} wins {Pot} chips with {ShowdownResults[WinnerIndex.Value]}");
        
        Pot = 0;
        Phase = Phase.HandComplete;

        // Check if match is complete (either 10 hands or someone is out of chips)
        if (HandNumber >= MaxHands || Players.Any(p => p.Chips == 0))
        {
            Phase = Phase.MatchComplete;
            var matchWinner = Players[0].Chips > Players[1].Chips ? 0 : 1;
            
            if (Players.Any(p => p.Chips == 0))
            {
                LogAction(matchWinner, PlayerAction.NextHand(), 
                         $"Match complete! Player {matchWinner + 1} wins - opponent ran out of chips");
            }
            else
            {
                LogAction(matchWinner, PlayerAction.NextHand(), 
                         $"Match complete after 10 hands! Player {matchWinner + 1} wins with {Players[matchWinner].Chips} chips");
            }
        }
    }

    private void CollectAntes()
    {
        foreach (var player in Players)
        {
            var actualAnte = player.CommitChips(AnteAmount);
            Pot += actualAnte;
        }
        
        // Reset committed amounts after antes
        foreach (var player in Players)
        {
            player.ResetStreet();
        }
        
        // Check if anyone is out of chips after antes
        if (Players.Any(p => p.Chips == 0))
        {
            Phase = Phase.MatchComplete;
            var matchWinner = Players[0].Chips > Players[1].Chips ? 0 : 1;
            LogAction(matchWinner, PlayerAction.NextHand(), 
                     $"Match complete! Player {matchWinner + 1} wins - opponent ran out of chips");
            return;
        }
    }

    private void DealCards()
    {
        // Deal 5 cards to each player alternating
        for (int i = 0; i < 5; i++)
        {
            Players[StartingPlayerIndex].DealCard(Deck.DrawOne());
            Players[1 - StartingPlayerIndex].DealCard(Deck.DrawOne());
        }
    }

    private void LogAction(int playerIndex, PlayerAction action, string description)
    {
        ActionLog.Add(new ActionLogEntry(HandNumber, playerIndex, action, description));
    }

    // Validation helpers for UI
    public bool CanCheck(int playerIndex) => 
        Phase == Phase.PreDrawBetting || Phase == Phase.PostDrawBetting ? 
        Betting!.CanCheck(Players[playerIndex]) : false;

    public bool CanBet(int playerIndex) => 
        Phase == Phase.PreDrawBetting || Phase == Phase.PostDrawBetting ? 
        Betting!.CanBet(Players[playerIndex]) : false;

    public bool CanCall(int playerIndex) => 
        Phase == Phase.PreDrawBetting || Phase == Phase.PostDrawBetting ? 
        Betting!.CanCall(Players[playerIndex]) : false;

    public bool CanRaise(int playerIndex) => 
        Phase == Phase.PreDrawBetting || Phase == Phase.PostDrawBetting ? 
        Betting!.CanRaise(Players[playerIndex]) : false;

    public bool CanDiscard(int playerIndex) => Phase == Phase.Draw;

    public int GetCurrentBet() => Betting?.CurrentBet ?? 0;
    public int GetToActPlayerIndex() => Betting?.ToActPlayerIndex ?? -1;
    public string GetLastActionDescription() => ActionLog.LastOrDefault()?.Description ?? "";
}
