using Backend.Models;
using Backend.DTOs;

namespace Backend.Services;

public sealed class GameService
{
    private readonly Dictionary<string, GameState> _games = new();

    public GameResponse CreateNewGame()
    {
        try
        {
            var gameId = Guid.NewGuid().ToString();
            var gameState = new GameState(gameId);
            
            _games[gameId] = gameState;
            
            // Start the first hand
            gameState.StartNewHand();
            
            var gameView = MapToGameView(gameState);
            
            return new GameResponse
            {
                Success = true,
                Game = gameView
            };
        }
        catch (Exception ex)
        {
            return new GameResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public GameResponse GetGameState(string gameId)
    {
        try
        {
            if (!_games.TryGetValue(gameId, out var gameState))
            {
                return new GameResponse
                {
                    Success = false,
                    Error = "Game not found"
                };
            }

            var gameView = MapToGameView(gameState);
            
            return new GameResponse
            {
                Success = true,
                Game = gameView
            };
        }
        catch (Exception ex)
        {
            return new GameResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public GameResponse ProcessAction(ActionRequest request)
    {
        try
        {
            if (!_games.TryGetValue(request.GameId, out var gameState))
            {
                return new GameResponse
                {
                    Success = false,
                    Error = "Game not found"
                };
            }

            var playerAction = MapToPlayerAction(request);
            gameState.ProcessAction(request.PlayerIndex, playerAction);
            
            var gameView = MapToGameView(gameState);
            
            return new GameResponse
            {
                Success = true,
                Game = gameView
            };
        }
        catch (Exception ex)
        {
            return new GameResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    private static PlayerAction MapToPlayerAction(ActionRequest request)
    {
        return request.ActionType.ToLower() switch
        {
            "check" => PlayerAction.Check(),
            "bet" => PlayerAction.Bet(request.Amount),
            "call" => PlayerAction.Call(),
            "raise" => PlayerAction.Raise(request.Amount),
            "fold" => PlayerAction.Fold(),
            "discard" => PlayerAction.Discard(request.CardIndices),
            "nexthand" => PlayerAction.NextHand(),
            _ => throw new ArgumentException($"Unknown action type: {request.ActionType}")
        };
    }

    private static GameView MapToGameView(GameState gameState)
    {
        var gameView = new GameView
        {
            GameId = gameState.GameId,
            Phase = gameState.Phase.ToString(),
            HandNumber = gameState.HandNumber,
            StartingPlayerIndex = gameState.StartingPlayerIndex,
            Pot = gameState.Pot,
            DeckRemaining = gameState.Deck.CountRemaining,
            LastEvent = gameState.GetLastActionDescription(),
            IsMatchComplete = gameState.Phase == Phase.MatchComplete
        };

        // Map players
        gameView.Players = gameState.Players.Select(p => new PlayerView
        {
            Index = p.PlayerIndex,
            Chips = p.Chips,
            HasFolded = p.HasFolded,
            Hand = p.Hand.Select(CardView.FromCard).ToList(),
            CommittedThisStreet = p.CommittedThisStreet
        }).ToList();

        // Map betting state
        if (gameState.Betting != null)
        {
            gameView.Betting = new BettingView
            {
                StreetIndex = gameState.Betting.StreetIndex,
                CurrentBet = gameState.Betting.CurrentBet,
                ToActPlayerIndex = gameState.Betting.ToActPlayerIndex,
                IsClosed = gameState.Betting.IsClosed
            };
        }

        // Map showdown results
        if (gameState.ShowdownResults != null && gameState.WinnerIndex.HasValue)
        {
            gameView.Showdown = new ShowdownView
            {
                WinnerIndex = gameState.WinnerIndex.Value,
                Hands = gameState.ShowdownResults.Select(h => new HandResultView
                {
                    HandType = h.Type.ToString(),
                    Description = h.Description,
                    PrimaryRanks = h.PrimaryRanks.Select(r => r.ToString()).ToList(),
                    Kickers = h.Kickers.Select(k => $"{k.Rank} of {k.Suit}").ToList()
                }).ToList()
            };
        }

        // Map action capabilities
        var currentPlayerIndex = gameState.GetToActPlayerIndex();
        if (currentPlayerIndex >= 0)
        {
            gameView.ActionCapabilities = new ActionCapabilities
            {
                CanCheck = gameState.CanCheck(currentPlayerIndex),
                CanBet = gameState.CanBet(currentPlayerIndex),
                CanCall = gameState.CanCall(currentPlayerIndex),
                CanRaise = gameState.CanRaise(currentPlayerIndex),
                CanFold = gameState.Phase == Phase.PreDrawBetting || gameState.Phase == Phase.PostDrawBetting,
                CanDiscard = gameState.CanDiscard(currentPlayerIndex),
                CanNextHand = gameState.Phase == Phase.HandComplete
            };
        }

        // Match winner
        if (gameState.Phase == Phase.MatchComplete)
        {
            gameView.MatchWinnerIndex = gameState.Players[0].Chips > gameState.Players[1].Chips ? 0 : 1;
        }

        // Draw phase active player
        if (gameState.Phase == Phase.Draw)
        {
            gameView.DrawPhaseActivePlayer = GetDrawPhaseActivePlayer(gameState);
        }

        return gameView;
    }

    private static int GetDrawPhaseActivePlayer(GameState gameState)
    {
        // Check who has already discarded by looking at the action log
        var startingPlayer = gameState.StartingPlayerIndex;
        var otherPlayer = 1 - startingPlayer;
        
        var startingPlayerDiscarded = gameState.ActionLog.Any(a => 
            a.HandNumber == gameState.HandNumber && 
            a.Action.Type == ActionType.Discard && 
            a.PlayerIndex == startingPlayer);
            
        var otherPlayerDiscarded = gameState.ActionLog.Any(a => 
            a.HandNumber == gameState.HandNumber && 
            a.Action.Type == ActionType.Discard && 
            a.PlayerIndex == otherPlayer);
        
        // If both have discarded, return -1 (shouldn't happen in Draw phase)
        if (startingPlayerDiscarded && otherPlayerDiscarded)
            return -1;
            
        // If starting player has discarded, it's the other player's turn
        if (startingPlayerDiscarded)
            return otherPlayer;
            
        // Otherwise, it's the starting player's turn
        return startingPlayer;
    }
}
