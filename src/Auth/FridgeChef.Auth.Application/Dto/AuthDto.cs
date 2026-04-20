namespace FridgeChef.Auth.Application.Dto;

public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

// Контекст клиента — передаётся при логине/регистрации для сохранения в refresh-токен.
public sealed record AuthClientContext(string? UserAgent, System.Net.IPAddress? Ip);
