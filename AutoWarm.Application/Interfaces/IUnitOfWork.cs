using System.Threading;
using System.Threading.Tasks;

namespace AutoWarm.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
