namespace FridgeChef.Auth.Application.Dto;

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public sealed record AuthClientContext(string? UserAgent, System.Net.IPAddress? Ip);
