using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Application.DTOs.Auth;
using AutoWarm.Application.Interfaces;
using AutoWarm.Domain.Entities;

namespace AutoWarm.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException("Email already in use.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return _tokenService.GenerateTokens(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        return _tokenService.GenerateTokens(user);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var principal = _tokenService.ValidateToken(request.RefreshToken);
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var userIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        return _tokenService.GenerateTokens(user);
    }
}
