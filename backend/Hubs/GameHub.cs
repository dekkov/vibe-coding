using Microsoft.AspNetCore.SignalR;
using Backend.Models;
using Backend.Services;
using Backend.DTOs;
using System.Security.Claims;

namespace Backend.Hubs;

public sealed class GameHub : Hub
{
    private readonly GameRoomService _gameRoomService;
    private readonly ILogger<GameHub> _logger;

    public GameHub(GameRoomService gameRoomService, ILogger<GameHub> logger)
    {
        _gameRoomService = gameRoomService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        
        // Find and remove the user from any room they were in
        await HandlePlayerDisconnect();
        
        await base.OnDisconnectedAsync(exception);
    }

    // Client → Server Events
    public async Task CreateRoom(string username)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                await Clients.Caller.SendAsync("Error", "Username cannot be empty");
                return;
            }

            var roomId = _gameRoomService.CreateRoom(username);
            
            // Add to group BEFORE joining the room so we receive the PlayerJoined event
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            
            // Join the room as the creator
            var success = await _gameRoomService.JoinRoom(roomId, username, Context.ConnectionId);
            
            if (success)
            {
                await Clients.Caller.SendAsync("RoomCreated", roomId);
                _logger.LogInformation("User {Username} created room {RoomId}", username, roomId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Failed to create room");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating room for user {Username}", username);
            await Clients.Caller.SendAsync("Error", "Failed to create room");
        }
    }

    public async Task JoinRoom(string roomId, string username)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                await Clients.Caller.SendAsync("Error", "Username cannot be empty");
                return;
            }

            if (string.IsNullOrWhiteSpace(roomId))
            {
                await Clients.Caller.SendAsync("Error", "Room ID cannot be empty");
                return;
            }

            // Add to group BEFORE joining the room so we receive the PlayerJoined event
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            
            var success = await _gameRoomService.JoinRoom(roomId, username, Context.ConnectionId);
            
            if (success)
            {
                await Clients.Caller.SendAsync("RoomJoined", roomId);
                
                // Send current room state ONLY if game is in progress
                var room = _gameRoomService.GetRoom(roomId);
                if (room != null && room.Status == GameStatus.InProgress)
                {
                    var viewer = room.Players.Values.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                    var gameView = viewer != null 
                        ? _gameRoomService.BuildGameViewFor(roomId, viewer.PlayerIndex)
                        : MapToGameView(room.GameState, room.Players, -1);
                    await Clients.Caller.SendAsync("GameStateUpdated", gameView);
                }
                
                _logger.LogInformation("User {Username} joined room {RoomId}", username, roomId);
            }
            else
            {
                await Clients.Caller.SendAsync("Error", "Failed to join room. Room may be full or not exist.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining room {RoomId} for user {Username}", roomId, username);
            await Clients.Caller.SendAsync("Error", "Failed to join room");
        }
    }

    public async Task LeaveRoom(string roomId)
    {
        try
        {
            // Find the username for this connection
            var room = _gameRoomService.GetRoom(roomId);
            if (room != null)
            {
                var player = room.Players.Values.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    await _gameRoomService.LeaveRoom(roomId, player.Username);
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
                    await Clients.Caller.SendAsync("RoomLeft");
                    _logger.LogInformation("User {Username} left room {RoomId}", player.Username, roomId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving room {RoomId}", roomId);
        }
    }

    public async Task PlayerAction(string roomId, PlayerAction action)
    {
        try
        {
            var room = _gameRoomService.GetRoom(roomId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Room not found");
                return;
            }

            var player = room.Players.Values.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found in room");
                return;
            }

            var success = await _gameRoomService.ProcessPlayerAction(roomId, player.Username, action);
            
            if (!success)
            {
                await Clients.Caller.SendAsync("Error", "Invalid action");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing action in room {RoomId}", roomId);
            await Clients.Caller.SendAsync("Error", "Failed to process action");
        }
    }

    public async Task PlayerReady(string roomId, bool isReady)
    {
        try
        {
            var room = _gameRoomService.GetRoom(roomId);
            if (room == null)
            {
                await Clients.Caller.SendAsync("Error", "Room not found");
                return;
            }

            var player = room.Players.Values.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
            if (player == null)
            {
                await Clients.Caller.SendAsync("Error", "Player not found in room");
                return;
            }

            var success = await _gameRoomService.SetPlayerReady(roomId, player.Username, isReady);
            
            if (!success)
            {
                await Clients.Caller.SendAsync("Error", "Failed to set ready status");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting ready status in room {RoomId}", roomId);
            await Clients.Caller.SendAsync("Error", "Failed to set ready status");
        }
    }

    public async Task GetActiveRooms()
    {
        try
        {
            var rooms = _gameRoomService.GetActiveRooms();
            await Clients.Caller.SendAsync("RoomsUpdated", rooms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active rooms");
            await Clients.Caller.SendAsync("Error", "Failed to get active rooms");
        }
    }

    public async Task CancelAutoAdvance(string roomId)
    {
        try
        {
            _gameRoomService.CancelAutoAdvance(roomId);
            await Clients.Caller.SendAsync("AutoAdvanceCancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling auto-advance for room {RoomId}", roomId);
            await Clients.Caller.SendAsync("Error", "Failed to cancel auto-advance");
        }
    }

    private async Task HandlePlayerDisconnect()
    {
        // Find all rooms this connection was in and remove the player
        var rooms = _gameRoomService.GetActiveRooms();
        foreach (var roomInfo in rooms)
        {
            var room = _gameRoomService.GetRoom(roomInfo.RoomId);
            if (room != null)
            {
                var player = room.Players.Values.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);
                if (player != null)
                {
                    await _gameRoomService.LeaveRoom(roomInfo.RoomId, player.Username);
                    _logger.LogInformation("Removed disconnected user {Username} from room {RoomId}", 
                        player.Username, roomInfo.RoomId);
                }
            }
        }
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
        var currentPlayer = currentPlayerIndex >= 0 && currentPlayerIndex < gameState.Players.Length 
            ? gameState.Players[currentPlayerIndex] 
            : null;

        gameView.ActionCapabilities = new ActionCapabilities
        {
            CanCheck = currentPlayer != null && (gameState.Betting?.CanCheck(currentPlayer) ?? false),
            CanBet = currentPlayer != null && (gameState.Betting?.CanBet(currentPlayer) ?? false),
            CanCall = currentPlayer != null && (gameState.Betting?.CanCall(currentPlayer) ?? false),
            CanRaise = currentPlayer != null && (gameState.Betting?.CanRaise(currentPlayer) ?? false),
            CanFold = currentPlayer != null && (gameState.Betting?.CanFold(currentPlayer) ?? false),
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
}
