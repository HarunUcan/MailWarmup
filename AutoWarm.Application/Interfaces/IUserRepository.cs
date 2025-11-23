using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoWarm.Domain.Entities;

namespace AutoWarm.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
