using System;

namespace AutoWarm.Application.DTOs.Auth;

public record AuthResponse(string Token, string RefreshToken, DateTime ExpiresAt);
