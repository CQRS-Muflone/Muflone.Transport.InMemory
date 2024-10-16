using Muflone.Messages.Commands;

namespace Muflone.Transport.InMemory.Abstracts;

public interface ISagaStartedByConsumer<in T> : IConsumer where T : Command
{
    Task ConsumeAsync(T message, CancellationToken cancellationToken = default);
}