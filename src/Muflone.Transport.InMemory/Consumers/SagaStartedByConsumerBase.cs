using Microsoft.Extensions.Logging;
using Muflone.Messages.Commands;
using Muflone.Transport.InMemory.Abstracts;

namespace Muflone.Transport.InMemory.Consumers;

public abstract class SagaStartedByConsumerBase<T> : ConsumerBase, ISagaStartedByConsumer<T>, IAsyncDisposable
    where T : Command
{
    public string TopicName { get; }
    
    	private readonly ILogger _logger;
    
    	protected abstract ICommandHandlerAsync<T> HandlerAsync { get; }

	    protected SagaStartedByConsumerBase(ILoggerFactory loggerFactory) : base(loggerFactory)
	    {
		    TopicName = typeof(T).Name;
    
		    _logger = loggerFactory.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
	    }
    
    	public Task ConsumeAsync(T message, CancellationToken cancellationToken = default)
    	{
    		if (message == null)
    			throw new ArgumentNullException(nameof(message));
    
    		return ConsumeAsyncCore(message, cancellationToken);
    	}
    
    	private async Task ConsumeAsyncCore<T>(T message, CancellationToken cancellationToken)
    	{
    		try
    		{
    			if (message == null)
    				throw new ArgumentNullException(nameof(message));
    
    			await HandlerAsync.HandleAsync((dynamic)message, cancellationToken);
    		}
    		catch (Exception ex)
    		{
    			_logger.LogError(ex, $"An error occurred processing command {typeof(T).Name}. StackTrace: {ex.StackTrace} - Source: {ex.Source} - Message: {ex.Message}");
    			throw;
    		}
    	}
    
    	public Task StartAsync(CancellationToken cancellationToken = default)
    	{
    		MufloneBroker.Commands.CollectionChanged += OnCommandReceived;
    
    		return Task.CompletedTask;
    	}
    
    	public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    
    	private void OnCommandReceived(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
    	{
    		if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
    		{
    			foreach (var item in args.NewItems)
    			{
    				if (item is T message)
    				{
    					Task.Run(async () => await ConsumeAsync(message));
    				}
    			}
    		}
    	}
    
    public ValueTask DisposeAsync()
    {
	    return ValueTask.CompletedTask;
    }
}