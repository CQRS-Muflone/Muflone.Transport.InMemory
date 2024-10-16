using Muflone.Messages.Events;

namespace Muflone.Transport.InMemory.Abstracts;

public interface ISagaEventConsumer<in T> : IConsumer where T : Event
{
    Task ConsumeAsync(T message, CancellationToken cancellationToken = default);
}