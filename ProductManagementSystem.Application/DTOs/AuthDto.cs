namespace ProductManagementSystem.Application.DTOs;

public record LoginRequest(string Username, string Password);

public record RegisterRequest(string Username, string Password, string Email);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer"
);

public record TokenPayload(
    int UserId,
    string Username,
    string Email
);
