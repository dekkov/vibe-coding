using Microsoft.AspNetCore.Mvc;
using Backend.Services;
using Backend.DTOs;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly GameService _gameService;

    public GameController(GameService gameService)
    {
        _gameService = gameService;
    }

    [HttpPost("new")]
    public ActionResult<GameResponse> CreateNewGame([FromBody] NewGameRequest request)
    {
        var response = _gameService.CreateNewGame();
        
        if (!response.Success)
        {
            return BadRequest(response);
        }
        
        return Ok(response);
    }

    [HttpGet("state")]
    public ActionResult<GameResponse> GetGameState([FromQuery] string gameId)
    {
        if (string.IsNullOrEmpty(gameId))
        {
            return BadRequest(new GameResponse
            {
                Success = false,
                Error = "GameId is required"
            });
        }

        var response = _gameService.GetGameState(gameId);
        
        if (!response.Success)
        {
            return NotFound(response);
        }
        
        return Ok(response);
    }

    [HttpPost("action")]
    public ActionResult<GameResponse> ProcessAction([FromBody] ActionRequest request)
    {
        if (string.IsNullOrEmpty(request.GameId))
        {
            return BadRequest(new GameResponse
            {
                Success = false,
                Error = "GameId is required"
            });
        }

        if (string.IsNullOrEmpty(request.ActionType))
        {
            return BadRequest(new GameResponse
            {
                Success = false,
                Error = "ActionType is required"
            });
        }

        var response = _gameService.ProcessAction(request);
        
        if (!response.Success)
        {
            return BadRequest(response);
        }
        
        return Ok(response);
    }
}
