using Muflone.Messages.Commands;

namespace Muflone.Transport.InMemory.Abstracts;

public interface ISagaStartedByAsync<in TCommand> : IDisposable where TCommand : Command
{
    Task StartedByAsync(TCommand command, CancellationToken cancellationToken = default);
}