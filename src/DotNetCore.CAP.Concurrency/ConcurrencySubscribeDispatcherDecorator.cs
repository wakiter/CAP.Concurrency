using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Persistence;

namespace DotNetCore.CAP.Concurrency
{
    internal sealed class ConcurrencySubscribeDispatcherDecorator : ISubscribeDispatcher
    {
        private readonly ISubscribeDispatcher _inner;
        private readonly ITryMarkingMessageAsInFlight _messageMaringAsInFlightTrier;
        private readonly IRemoveInFlightMarkFromMessage _messageInFlightMarkerRemover;

        public ConcurrencySubscribeDispatcherDecorator(
            ISubscribeDispatcher inner,
            ITryMarkingMessageAsInFlight messageMaringAsInFlightTrier, 
            IRemoveInFlightMarkFromMessage messageInFlightMarkerRemover)
        {
            _inner = inner;
            _messageMaringAsInFlightTrier = messageMaringAsInFlightTrier;
            _messageInFlightMarkerRemover = messageInFlightMarkerRemover;
        }

        public async Task<OperateResult> DispatchAsync(
            MediumMessage message, 
            CancellationToken cancellationToken = new CancellationToken())
        {
            var marked = await _messageMaringAsInFlightTrier.TryMarkingAsInFlight(message, cancellationToken);
            if (!marked)
            {
                return OperateResult.Failed(
                    null!,
                    new OperateError
                    {
                        Code = "MessageAlreadyInFlight",
                        Description = "Other process is already handling this message and it's skipped here"
                    });
            }

            try
            {
                return await _inner.DispatchAsync(message, cancellationToken);
            }
            catch 
            {
                await _messageInFlightMarkerRemover.RemoveInFlightMark(message, cancellationToken);
                throw;
            }
        }

        public async Task<OperateResult> DispatchAsync(
            MediumMessage message, 
            ConsumerExecutorDescriptor descriptor,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var marked = await _messageMaringAsInFlightTrier.TryMarkingAsInFlight(message, cancellationToken);
            if (!marked)
            {
                return OperateResult.Failed(
                    null!,
                    new OperateError
                    {
                        Code = "MessageAlreadyInFlight",
                        Description = "Other process is already handling this message and it's skipped here"
                    });
            }

            try
            {
                return await _inner.DispatchAsync(message, descriptor, cancellationToken);
            }
            catch
            {
                await _messageInFlightMarkerRemover.RemoveInFlightMark(message, cancellationToken);
                throw;
            }
        }
    }
}