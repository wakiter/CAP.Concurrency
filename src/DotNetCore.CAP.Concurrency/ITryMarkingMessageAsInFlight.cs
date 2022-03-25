using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Concurrency
{
    public interface ITryMarkingMessageAsInFlight
    {
        Task<bool> TryMarkingAsInFlight(MediumMessage message, CancellationToken cancellationToken = new CancellationToken());
    }
}