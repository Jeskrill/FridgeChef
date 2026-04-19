namespace FridgeChef.Application.Auth.Dto;

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);
