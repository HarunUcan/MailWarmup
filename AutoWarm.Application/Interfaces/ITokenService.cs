using System;
using System.Security.Claims;
using AutoWarm.Application.DTOs.Auth;
using AutoWarm.Domain.Entities;

namespace AutoWarm.Application.Interfaces;

public interface ITokenService
{
    AuthResponse GenerateTokens(User user);
    ClaimsPrincipal? ValidateToken(string token);
}
