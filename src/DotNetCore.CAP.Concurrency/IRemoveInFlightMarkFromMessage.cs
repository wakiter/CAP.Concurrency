using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Concurrency
{
    public interface IRemoveInFlightMarkFromMessage
    {
        Task RemoveInFlightMark(MediumMessage message, CancellationToken cancellationToken = new CancellationToken());
    }
}