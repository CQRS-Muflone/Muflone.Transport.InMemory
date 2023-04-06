using Microsoft.Extensions.DependencyInjection;
using Muflone.Persistence;
using Muflone.Transport.InMemory.Abstracts;

namespace Muflone.Transport.InMemory;

public static class MufloneTransportInMemoryHelper
{
	public static IServiceCollection AddMufloneTransportInMemory(this IServiceCollection services,
		IEnumerable<IConsumer> messageConsumers)
	{
		foreach (var consumer in messageConsumers)
		{
			consumer.StartAsync(CancellationToken.None);
		}

		services.AddSingleton<IServiceBus, ServiceBus>();
		services.AddSingleton<IEventBus, ServiceBus>();

		return services;
	}
}