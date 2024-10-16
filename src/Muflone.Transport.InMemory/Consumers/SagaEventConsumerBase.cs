using Microsoft.Extensions.Logging;
using Muflone.Messages.Events;
using Muflone.Persistence;
using Muflone.Transport.InMemory.Abstracts;

namespace Muflone.Transport.InMemory.Consumers;

public abstract class SagaEventConsumerBase<T>(
	ILoggerFactory loggerFactory,
	ISerializer messageSerializer,
	ILogger logger,
	string topicName)
	: ConsumerBase(loggerFactory), ISagaEventConsumer<T>, IAsyncDisposable where T : Event
{
	public string TopicName { get; } = topicName;

	private readonly ISerializer _messageSerializer = messageSerializer;

	protected abstract IEnumerable<ISagaEventConsumer<T>> HandlersAsync { get; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
    	MufloneBroker.Events.CollectionChanged += OnEventReceived;

    	return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task ConsumeAsync(T message, CancellationToken cancellationToken = default)
    {
    	try
    	{
    		if (message == null)
    			throw new ArgumentNullException(nameof(message));

    		foreach (var handlerAsync in HandlersAsync)
    		{
    			await handlerAsync.ConsumeAsync((dynamic)message, cancellationToken);
    		}
    	}
    	catch (Exception ex)
    	{
    		logger.LogError(ex,
    			$"An error occurred processing domainEvent {typeof(T).Name}. StackTrace: {ex.StackTrace} - Source: {ex.Source} - Message: {ex.Message}");
    		throw;
    	}
    }

    private void OnEventReceived(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs @event)
    {
    	if (@event.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
    	{
    		foreach (var item in @event.NewItems)
    		{
    			if (item is T message)
    			{
    				Task.Run(async () => await ConsumeAsync(message));
    			}
    		}
    	}
    }

    #region Dispose
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #endregion
}