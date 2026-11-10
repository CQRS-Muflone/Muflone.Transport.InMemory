# Muflone.Transport.InMemory

[![NuGet](https://img.shields.io/nuget/v/Muflone.Transport.InMemory)](https://www.nuget.org/packages/Muflone.Transport.InMemory)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

InMemory transport for [Muflone](https://github.com/CQRS-Muflone), providing an in-process service bus to dispatch commands and publish events without any external broker dependency. Ideal for development, testing, and lightweight production scenarios.

---

## Requirements

- .NET 10.0+
- `Muflone` 10.0.1+

---

## Installation

**Package Manager**
```powershell
Install-Package Muflone.Transport.InMemory
```

**.NET CLI**
```bash
dotnet add package Muflone.Transport.InMemory
```

---

## Getting Started

### 1. Register the transport

Call `AddMufloneTransportInMemory` on your `IServiceCollection` during application startup:

```csharp
builder.Services.AddMufloneTransportInMemory();
```

This registers the following services:

| Service | Implementation | Lifetime |
|---|---|---|
| `IMessageSubscriber` | `InMemorySubscriber` | Singleton |
| `IServiceBus` | `InMemoryBus` | Singleton |
| `IEventBus` | `InMemoryBus` | Singleton |
| `MessageHandlersStarter` | Hosted service | — |

### 2. Register your handlers

Register your command and domain event handlers with the `AddCommandHandler` / `AddDomainEventHandler` extension methods (defined in `Muflone` core, in `MessageHandlerExtension`). They are transport-agnostic — the same methods you use with any Muflone transport:

```csharp
builder.Services.AddMufloneTransportInMemory();

builder.Services.AddCommandHandler<CreateOrderCommandHandler>();
builder.Services.AddDomainEventHandler<OrderCreatedEventHandler>();
```

Each handler type implements the usual Muflone handler interface, e.g.:

```csharp
public class CreateOrderCommandHandler(IRepository repository, ILoggerFactory loggerFactory)
    : CommandHandlerAsync<CreateOrder>(repository, loggerFactory)
{
    public override Task HandleAsync(CreateOrder command, CancellationToken cancellationToken = default)
    {
        // handle the command
        return Task.CompletedTask;
    }
}
```

At host startup the `MessageHandlersStarter` hosted service (registered by `AddMufloneTransportInMemory`) reads every registered handler type and subscribes it on `InMemorySubscriber`. No extra wiring is required — do **not** register `MessageHandlersStarter` yourself.

> **Note:** the registered handler types are tracked in a static list inside `Muflone` core (`MessageHandlerExtension`) that is shared process-wide and never cleared. This is transparent for a normal application host, but be aware of it in test suites that build multiple hosts within a single process.

### 3. Send a command

Inject `IServiceBus` and call `SendAsync`:

```csharp
public class MyService(IServiceBus serviceBus)
{
    public async Task DoSomethingAsync(CancellationToken cancellationToken)
    {
        var command = new MyCommand(Guid.NewGuid());
        await serviceBus.SendAsync(command, cancellationToken);
    }
}
```

### 4. Publish an event

Inject `IEventBus` and call `PublishAsync`:

```csharp
public class MyService(IEventBus eventBus)
{
    public async Task NotifyAsync(CancellationToken cancellationToken)
    {
        var @event = new MyEvent(Guid.NewGuid());
        await eventBus.PublishAsync(@event, cancellationToken);
    }
}
```

---

## How It Works

### InMemoryBus

`InMemoryBus` implements both `IServiceBus` (commands) and `IEventBus` (events). When a message is dispatched, it looks up registered subscribers via `InMemorySubscriber` and writes the message directly into each subscriber's .NET `Channel<object>`.

### InMemorySubscriber

`InMemorySubscriber` maintains a `ConcurrentDictionary` mapping message types to their `IInMemoryChannel` instances. Each subscriber gets an unbounded `System.Threading.Channels.Channel` that is read asynchronously and forwarded to the corresponding handler. Namespace-agnostic lookup is also supported, so commands/events with the same type name but different namespaces are matched correctly.

### MufloneBroker (static)

`MufloneBroker` provides a lightweight static alternative that stores dispatched messages in `ObservableCollection` properties — useful in tests or simple scenarios:

```csharp
MufloneBroker.Send(command);    // adds to MufloneBroker.Commands
MufloneBroker.Publish(@event);  // adds to MufloneBroker.Events
```

---

## Sample Project

See a working example at [BrewUp](https://github.com/BrewUp/BrewUp).

---

## Authors

- Alberto Acerbis
- Alessandro Colla

## License

This project is licensed under the [MIT License](LICENSE).
