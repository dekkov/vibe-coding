using Microsoft.AspNetCore.Mvc;
using Backend.Services;

namespace Backend.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly GameRoomService _gameRoomService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(GameRoomService gameRoomService, ILogger<HealthController> logger)
    {
        _gameRoomService = gameRoomService;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "17-poker-backend"
        });
    }

    /// <summary>
    /// Readiness check - verifies all dependencies are available
    /// </summary>
    [HttpGet("ready")]
    public IActionResult Ready()
    {
        try
        {
            // Check if game room service is operational
            var rooms = _gameRoomService.GetActiveRooms();
            
            return Ok(new
            {
                status = "ready",
                timestamp = DateTime.UtcNow,
                activeRooms = rooms.Count,
                dependencies = new
                {
                    gameRoomService = "operational"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new
            {
                status = "not_ready",
                timestamp = DateTime.UtcNow,
                error = "Service Unavailable"
            });
        }
    }

    /// <summary>
    /// Liveness check - verifies the application is running
    /// </summary>
    [HttpGet("live")]
    public IActionResult Live()
    {
        return Ok(new
        {
            status = "alive",
            timestamp = DateTime.UtcNow,
            uptime = Environment.TickCount64 / 1000 // seconds
        });
    }

    /// <summary>
    /// Detailed status information
    /// </summary>
    [HttpGet("status")]
    public IActionResult Status()
    {
        var rooms = _gameRoomService.GetActiveRooms();
        
        return Ok(new
        {
            status = "operational",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            uptime = Environment.TickCount64 / 1000,
            statistics = new
            {
                activeRooms = rooms.Count,
                waitingRooms = rooms.Count(r => r.Status == Models.GameStatus.Waiting),
                inProgressRooms = rooms.Count(r => r.Status == Models.GameStatus.InProgress),
                totalPlayers = rooms.Sum(r => r.PlayerCount)
            },
            memory = new
            {
                totalMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024,
                gen0Collections = GC.CollectionCount(0),
                gen1Collections = GC.CollectionCount(1),
                gen2Collections = GC.CollectionCount(2)
            }
        });
    }
}

