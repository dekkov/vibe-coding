using System;
using Microsoft.AspNetCore.SignalR;
using Backend.Models;
using Backend.DTOs;
using Backend.Hubs;
using System.Collections.Concurrent;

namespace Backend.Services;

public sealed class GameRoomService : IDisposable
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    private readonly Timer _cleanupTimer;
    private readonly IHubContext<GameHub> _hubContext;
    private readonly ILogger<GameRoomService> _logger;
    private readonly ConcurrentDictionary<string, Timer> _autoAdvanceTimers = new();

    public GameRoomService(IHubContext<GameHub> hubContext, ILogger<GameRoomService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
        
        // Cleanup inactive rooms every minute
        _cleanupTimer = new Timer(CleanupInactiveRooms, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public string CreateRoom(string username)
    {
        string roomId;
        int attempts = 0;
        
        do
        {
            roomId = GenerateRoomCode();
            attempts++;
            
            if (attempts > 100) // Safety check
                throw new InvalidOperationException("Unable to generate unique room code");
                
        } while (_rooms.ContainsKey(roomId));
        
        var gameState = new GameState(roomId);
        var room = new GameRoom
        {
            RoomId = roomId,
            GameState = gameState,
            Status = GameStatus.Waiting,
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            Players = new Dictionary<string, PlayerConnection>()
        };
        
        _rooms[roomId] = room;
        _logger.LogInformation("Created room {RoomId} by user {Username}", roomId, username);
        
        return roomId;
    }

    public async Task<bool> JoinRoom(string roomId, string username, string connectionId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            _logger.LogWarning("Attempted to join non-existent room {RoomId}", roomId);
            return false;
        }

        int playerIndex;
        lock (room)
        {
            if (room.IsFull)
            {
                _logger.LogWarning("Attempted to join full room {RoomId}", roomId);
                return false;
            }

            if (room.Players.ContainsKey(username))
            {
                _logger.LogWarning("Username {Username} already exists in room {RoomId}", username, roomId);
                return false;
            }

            // Assign player index (0 or 1)
            playerIndex = room.PlayerCount;
            
            var playerConnection = new PlayerConnection
            {
                Username = username,
                ConnectionId = connectionId,
                PlayerIndex = playerIndex,
                IsReady = false,
                ConnectedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            room.Players[username] = playerConnection;
        }
        
        room.LastActivity = DateTime.UtcNow;

        _logger.LogInformation("User {Username} joined room {RoomId} as player {PlayerIndex}", 
            username, roomId, playerIndex);

        // Send information about all existing players to the new joiner
        foreach (var existingPlayer in room.Players.Values)
        {
            if (existingPlayer.ConnectionId != connectionId) // Don't send their own info
            {
                await _hubContext.Clients.Client(connectionId).SendAsync("PlayerJoined", existingPlayer.Username, existingPlayer.PlayerIndex);
            }
        }
        
        // Notify all clients in the room about the new player
        await _hubContext.Clients.Group(roomId).SendAsync("PlayerJoined", username, playerIndex);
        
        // Update room list for all clients
        await BroadcastRoomList();

        return true;
    }

    public async Task<bool> LeaveRoom(string roomId, string username)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            return false;

        if (!room.Players.Remove(username, out var player))
            return false;

        _logger.LogInformation("User {Username} left room {RoomId}", username, roomId);

        // If room is empty, remove it
        if (room.IsEmpty)
        {
            _rooms.TryRemove(roomId, out _);
            _logger.LogInformation("Removed empty room {RoomId}", roomId);
        }
        else
        {
            // Notify remaining players
            await _hubContext.Clients.Group(roomId).SendAsync("PlayerLeft", username);
        }

        // Update room list
        await BroadcastRoomList();

        return true;
    }

    public async Task<bool> SetPlayerReady(string roomId, string username, bool isReady)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            return false;

        if (!room.Players.TryGetValue(username, out var player))
            return false;

        player.IsReady = isReady;
        player.LastActivity = DateTime.UtcNow;
        room.LastActivity = DateTime.UtcNow;

        _logger.LogInformation("User {Username} set ready status to {IsReady} in room {RoomId}", 
            username, isReady, roomId);

        // Check if game can start
        if (room.CanStart)
        {
            await StartGame(roomId);
        }

        // Notify room about ready status change
        await _hubContext.Clients.Group(roomId).SendAsync("PlayerReadyChanged", username, isReady);

        return true;
    }

    public async Task<bool> ProcessPlayerAction(string roomId, string username, PlayerAction action)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            return false;

        if (!room.Players.TryGetValue(username, out var player))
            return false;

        if (room.Status != GameStatus.InProgress)
            return false;

        try
        {
            room.GameState.ProcessAction(player.PlayerIndex, action);
            player.LastActivity = DateTime.UtcNow;
            room.LastActivity = DateTime.UtcNow;

            // Broadcast updated game state
            await BroadcastGameState(roomId);

            // Check if game/match is complete
            if (room.GameState.Phase == Phase.MatchComplete)
            {
                await HandleMatchComplete(roomId);
            }
            else if (room.GameState.Phase == Phase.HandComplete && !room.GameState.IsMatchComplete)
            {
                ScheduleAutoAdvance(roomId, TimeSpan.FromSeconds(5));
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing action for user {Username} in room {RoomId}", 
                username, roomId);
            return false;
        }
    }

    public GameRoom? GetRoom(string roomId)
    {
        return _rooms.TryGetValue(roomId, out var room) ? room : null;
    }

    public List<RoomInfo> GetActiveRooms()
    {
        return _rooms.Values
            .Where(r => r.Status != GameStatus.Complete)
            .Select(r => new RoomInfo
            {
                RoomId = r.RoomId,
                PlayerCount = r.PlayerCount,
                MaxPlayers = r.MaxPlayers,
                Status = r.Status,
                HandNumber = r.GameState.HandNumber,
                Pot = r.GameState.Pot,
                CreatedAt = r.CreatedAt,
                PlayerNames = r.Players.Values.Select(p => p.Username).ToList()
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }

    private async Task StartGame(string roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            return;

        room.Status = GameStatus.InProgress;
        room.GameState.StartNewHand();

        _logger.LogInformation("Started game in room {RoomId}", roomId);

        // Notify all players that game started
        await _hubContext.Clients.Group(roomId).SendAsync("GameStarted");
        
        // Broadcast initial game state
        await BroadcastGameState(roomId);
    }

    private async Task HandleMatchComplete(string roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            return;

        room.Status = GameStatus.Complete;

        if (!room.GameState.WinnerIndex.HasValue)
        {
            _logger.LogError("Match completed in room {RoomId} without a winner.", roomId);
            throw new InvalidOperationException("Match completed without a determined winner.");
        }

        var winnerIndex = room.GameState.WinnerIndex.Value;
        var winnerConnection = room.Players.Values
            .FirstOrDefault(p => p.PlayerIndex == winnerIndex);
        if (winnerConnection == null)
        {
            _logger.LogWarning("Winner player {WinnerIndex} not found in room {RoomId} connections", winnerIndex, roomId);
        }
        var winnerUsername = room.Players.Values
            .FirstOrDefault(p => p.PlayerIndex == winnerIndex)?.Username ?? "";

        var matchResult = new MatchResult
        {
            WinnerIndex = winnerIndex,
            WinnerUsername = winnerUsername,
            WinnerChips = room.GameState.Players[winnerIndex].Chips,
            FinalPot = room.GameState.Pot,
            TotalHands = room.GameState.HandNumber,
            CompletedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Match completed in room {RoomId}. Winner: {WinnerUsername}", 
            roomId, matchResult.WinnerUsername);

        // Notify all players about match completion
        await _hubContext.Clients.Group(roomId).SendAsync("MatchComplete", matchResult);
    }

    private async Task BroadcastGameState(string roomId)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
            return;

        // Send personalized views to each connected player
        var playersSnapshot = room.Players.Values.ToList();
        foreach (var player in playersSnapshot)
        {
            var personalized = MapToGameView(room.GameState, room.Players, player.PlayerIndex);
            await _hubContext.Clients.Client(player.ConnectionId).SendAsync("GameStateUpdated", personalized);
        }
    }

    private async Task BroadcastRoomList()
    {
        var activeRooms = GetActiveRooms();
        await _hubContext.Clients.All.SendAsync("RoomsUpdated", activeRooms);
    }

    private void CleanupInactiveRooms(object? state)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-3);
        var inactiveRooms = _rooms.Values
            .Where(r => r.LastActivity < cutoff && r.Status != GameStatus.InProgress)
            .ToList();

        foreach (var room in inactiveRooms)
        {
            _logger.LogInformation("Cleaning up inactive room {RoomId}", room.RoomId);
            
            // Notify all players in room
            _ = NotifyRoomCleanup(room.RoomId);
            _rooms.TryRemove(room.RoomId, out _);
        }
    }

    private async Task NotifyRoomCleanup(string roomId)
    {
        await _hubContext.Clients.Group(roomId).SendAsync("RoomTerminated", 
            "Room inactive for 3 minutes. Returning to lobby.");
    }

    private string GenerateRoomCode()
    {
        // 6-character alphanumeric, avoiding confusing characters
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No 0, O, I, 1
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
    }

    public GameView BuildGameViewFor(string roomId, int viewerIndex)
    {
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            throw new KeyNotFoundException("Room not found");
        }
        return MapToGameView(room.GameState, room.Players, viewerIndex);
    }

    private static GameView MapToGameView(GameState gameState, Dictionary<string, PlayerConnection> players, int viewerIndex)
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

        // Map players with usernames
        gameView.Players = gameState.Players.Select(p =>
        {
            var isViewer = p.PlayerIndex == viewerIndex;
            var revealHands = gameState.Phase == Phase.HandComplete && gameState.ShowdownResults != null;
            var hand = (isViewer || revealHands)
                ? p.Hand.Select(CardView.FromCard).ToList()
                : p.Hand.Select(_ => new CardView { Rank = null, Suit = null, IsJoker = false }).ToList();

            return new PlayerView
            {
                Index = p.PlayerIndex,
                Username = players.Values.FirstOrDefault(conn => conn.PlayerIndex == p.PlayerIndex)?.Username ?? $"Player {p.PlayerIndex + 1}",
                Chips = p.Chips,
                HasFolded = p.HasFolded,
                Hand = hand,
                CommittedThisStreet = p.CommittedThisStreet
            };
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
        gameView.ActionCapabilities = new ActionCapabilities
        {
            CanCheck = gameState.Betting?.CanCheck(gameState.Players[currentPlayerIndex]) ?? false,
            CanBet = gameState.Betting?.CanBet(gameState.Players[currentPlayerIndex]) ?? false,
            CanCall = gameState.Betting?.CanCall(gameState.Players[currentPlayerIndex]) ?? false,
            CanRaise = gameState.Betting?.CanRaise(gameState.Players[currentPlayerIndex]) ?? false,
            CanFold = gameState.Betting?.CanFold(gameState.Players[currentPlayerIndex]) ?? false,
            CanDiscard = gameState.Phase == Phase.Draw,
            CanNextHand = gameState.Phase == Phase.HandComplete && !gameState.IsMatchComplete
        };

        // Set draw phase active player
        gameView.DrawPhaseActivePlayer = GetDrawPhaseActivePlayer(gameState);

        return gameView;
    }

    private static int GetDrawPhaseActivePlayer(GameState gameState)
    {
        if (gameState.Phase != Phase.Draw)
            return -1;

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
        
        if (startingPlayerDiscarded && otherPlayerDiscarded)
            return -1; // Both done
            
        if (startingPlayerDiscarded)
            return otherPlayer; // Starting player has discarded, it's the other player's turn
            
        return startingPlayer; // Otherwise, it's the starting player's turn
    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        foreach (var kvp in _autoAdvanceTimers)
        {
            kvp.Value.Dispose();
        }
    }

    public void CancelAutoAdvance(string roomId)
    {
        if (_autoAdvanceTimers.TryRemove(roomId, out var timer))
        {
            timer.Dispose();
            _logger.LogInformation("Auto-advance cancelled for room {RoomId}", roomId);
        }
    }

    private void ScheduleAutoAdvance(string roomId, TimeSpan delay)
    {
        CancelAutoAdvance(roomId);
        var timer = new Timer(async _ =>
        {
            try
            {
                if (!_rooms.TryGetValue(roomId, out var room)) return;
                if (room.Status != GameStatus.InProgress) return;
                if (room.GameState.Phase != Phase.HandComplete || room.GameState.IsMatchComplete) return;

                room.GameState.StartNewHand();
                room.LastActivity = DateTime.UtcNow;
                _logger.LogInformation("Auto-advanced to next hand in room {RoomId}", roomId);
                await BroadcastGameState(roomId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-advance for room {RoomId}", roomId);
            }
            finally
            {
                CancelAutoAdvance(roomId);
            }
        }, null, delay, Timeout.InfiniteTimeSpan);

        _autoAdvanceTimers[roomId] = timer;
    }
}
