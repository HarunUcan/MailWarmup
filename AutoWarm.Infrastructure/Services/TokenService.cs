using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoWarm.Application.DTOs.Auth;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;
using AutoWarm.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AutoWarm.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly JwtSecurityTokenHandler _handler = new();
    private readonly byte[] _key;

    public TokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _key = Encoding.UTF8.GetBytes(_options.SigningKey);
    }

    public AuthResponse GenerateTokens(User user)
    {
        var accessExpires = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var refreshExpires = DateTime.UtcNow.AddMinutes(_options.RefreshTokenMinutes);

        var accessToken = CreateToken(user, accessExpires);
        var refreshToken = CreateToken(user, refreshExpires, isRefresh: true);

        return new AuthResponse(accessToken, refreshToken, accessExpires);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var principal = _handler.ValidateToken(token, BuildValidationParameters(), out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private string CreateToken(User user, DateTime expires, bool isRefresh = false)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("typ", isRefresh ? "refresh" : "access")
        };

        var credentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return _handler.WriteToken(token);
    }

    private TokenValidationParameters BuildValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(_key),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    }
}
