using System.Threading;
using System.Threading.Tasks;

namespace AutoWarm.Application.Interfaces;

public interface IWarmupEngine
{
    Task GenerateDailyJobsAsync(CancellationToken cancellationToken = default);
    Task ExecutePendingJobsAsync(CancellationToken cancellationToken = default);
}
