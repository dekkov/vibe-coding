using System.Text.Json;
using Backend.Models;

namespace Backend.Middleware;

public class ValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidationMiddleware> _logger;

    public ValidationMiddleware(RequestDelegate next, ILogger<ValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation for SignalR endpoints
        if (context.Request.Path.StartsWithSegments("/gamehub"))
        {
            await _next(context);
            return;
        }

        // Validate content type for POST/PUT requests
        if ((context.Request.Method == "POST" || context.Request.Method == "PUT") &&
            context.Request.Path.StartsWithSegments("/api"))
        {
            if (!context.Request.ContentType?.Contains("application/json") ?? true)
            {
                context.Response.StatusCode = 415; // Unsupported Media Type
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Content-Type must be application/json"
                });
                return;
            }

            // Validate request body size (max 1MB)
            if (context.Request.ContentLength > 1_048_576)
            {
                context.Response.StatusCode = 413; // Payload Too Large
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Request body too large. Maximum size is 1MB."
                });
                return;
            }
        }

        // Validate room code format for room-related endpoints
        if (context.Request.Path.Value?.Contains("/room/") ?? false)
        {
            var segments = context.Request.Path.Value.Split('/');
            var roomCodeIndex = Array.IndexOf(segments, "room") + 1;
            
            if (roomCodeIndex < segments.Length)
            {
                var roomCode = segments[roomCodeIndex];
                if (!IsValidRoomCode(roomCode))
                {
                    context.Response.StatusCode = 400;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Invalid room code format. Must be 6 alphanumeric characters."
                    });
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool IsValidRoomCode(string code)
    {
        return code.Length == 6 &&
               code.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z'));
    }
}

public static class ValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ValidationMiddleware>();
    }
}

