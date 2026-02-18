# Oscillation

## Introduction
`Oscillation` is a set of libraries for building distributed signal scheduling middlewares.
By signal scheduling, I mean a thin layer for scheduling one-time message publications, where the messages are stored in some durable store to be fired in the future.
Although this repository can be used for one-time job scheduling, it's recommended to use this repository for signal scheduling only.

## Architecture
In `Oscillation` there are two main components: the **signal store**, and the **signal distributors**.

Submitting a signal is simply adding it to the **signal store**.
The **signal distributors** regularly check for ready-to-fire signals, and distribute them via the distribution gateway.

Signals have 4 state through out their lifecycle: PENDING, PROCESSING, SUCCESS, FAILED.
1. When a signal is submitted, the signal is in PENDING state.
2. When a signal is ready-to-fire, a **signal distributor** picks it and transitions it into PROCESSING state.
3. If the **signal distributor** processes it successfully, then the signal will end up with SUCCESS state.
4. If not, the signal will be transitioned into PENDING state to be processed later.
5. If it takes too much time to process the signal, the processing attempt will be considered failed.
6. If after some attempts the signal isn't still processed successfully, then the signal will end up with FAILED state.
7. A finalized signal retains for some period of time.

Inspired by how low-level systems handle single hardware timer, the library uses notification as an optimization for the polling process:
1. Each **signal distributor** maintains an "internal timer" to know when to check the **signal store**.
2. When a signal is submitted, a notification is published to every **signal distributor** about the fire time.
3. The **signal distributors** listen for notifications and adjust their internal timers.
4. After each poll, the **signal distributors** check for the next fire time from the **signal store** to adjust their internal timers.

The provided **signal distributor** implementation is guaranteed to work correctly in distributed manner with the assumption is that the clocks of the nodes are regularly synchronized, although the efficiency depends heavily on the implementation of the **signal store**.

## Usage
You must provide implementations for these 4 interfaces: `ISignalStore`, `IDistributionGateway`, `IDistributionPolicyProvider`, and `ITimeProvider`.

The requirement for `ITimeProvider` can easily be fulfilled by providing an adapter wrapping an `TimeProvider` implementation. Note that we target .NET Standard 2.1, so we don't have `TimeProvider` right away.

The requirement for `IDistributionPolicyProvider` can easily be fulfilled also by providing a Dictionary-based implementation.

The requirement for `ISignalStore` can easily be fulfilled by providing an EF Core implementation since the interface is heavily inspired by unit of work pattern, although some twists are needed to make it efficient.

The only requirement left is `IDistributionGateway`, which varies a lot depending on the situation, so we decide to let users provide their own, which is easy enough if you are not a vibe coder.

When these requirements are fulfilled, the only thing you need to do is running background jobs to run the following components:
1. `SignalDistributor` is responsible for scheduling signals, you run `StartAsync` to start the loop, and run `StartAsync` on notification. Note that only one active `StartAsync` is allowed per `SignalDistributor` instance.
2. `ZombieSignalProcessor` is responsible for processing zombie signals, you run `StartAsync` to start the loop. Unlike the `SignalDistributor`, you can have as many active `StartAsync` as you want.
3. `DeadSignalProcessor` is responsible for processing dead signals, you run `StartAsync` to start the loop. Unlike the `SignalDistributor`, you can have as many active `StartAsync` as you want.

How many instances to use and how each instance is configured is totally up to you.

Currently, we haven't implemented graceful shutdown, this capability will available in the near future. 

## Hosting
The repository provides a hosting framework to streamline the configuration.

### Client hosting
The `Oscillation.Hosting.Client` library provides a template for submitting signals, which is configured as follows:
```csharp
services.AddOscillationClient(oscillation => 
{
    oscillation
        .UseSignalStore(provider => ...)
        .UseSignalNotificationPublisher(provider => ...);
});
```
With this configuration, the `SignalSubmissionTemplate` will be registered as a singleton service.

### Server hosting
The `Oscillation.Hosting.Server` library provides a background server scheduling signals, which is configured as follows:
```csharp
services.AddOscillationServer(oscillation => 
{
    oscillation
        .UseSignalStore(provider => ...)
        .UseDistributionGateway(provider => ...)
        .UseDistributionPolicyProvider(provider => ...)
        .UseTimeProvider(provider => ...)
        .UseSignalNotificationSubscriber(provider => ...)
        .SetNumberOfDistributors(provider => ...)
        .ConfigureSignalDistributorOptions((provider, options) => ...)
        .ConfigureZombieSignalProcessorOptions((provider, options) => ...)
        .ConfigureDeadSignalProcessorOptions((provider, options) => ...);
});
```
With this configuration, a background service called `SignalProcessingBackgroundService` will be registered with `n` **signal distributors** running concurrently where `n` is configured by `SetNumberOfDistributors`.

The library provides default implementations for `ITimeProvider` and `IDistributionPolicyProvider` which are configured as follows:
```csharp
oscillation
    .UseDefaultTimeProvider()
    .UseDefaultDistributionPolicyProvider(policyProviderBuilder => 
    {
        policyProviderBuilder
            .SetDefaultPolicy(...)
            .Register("rabbitmq", ...)
            .Register("kafka", ...)
            .Register("NATS", ...);
    });
```

## Extensions
### Entity Framework Core
The `Oscillation.Stores.EntityFrameworkCore.Hosting` library provides some extension methods to configure Entity Framework Core backed signal store for both client hosting and server hosting:
```csharp
services.AddOscillation[Client|Server](oscillation => 
{
    oscillation
        .UseEntityFrameworkCoreSignalStore(signalStoreBuilder => 
        {
            signalStoreBuilder
                .UseDbContextFactory(provider => ...)
                .UseSelectTemplateProvider(provider => ...)
        });
});
```
The library provides two implementations for `ISignalStoreDbContextFactory`, one use a standalone `DbContext`, another use an existing `IDbContextFactory`.

For the standalone `DbContext` one, configure as follows:
```csharp
oscillation
    .UseEntityFrameworkCoreSignalStore(signalStoreBuilder => 
    {
        signalStoreBuilder
            .UseStandaloneDbContextFactory((provider, dbContextFactoryBuilder) => 
            {
                dbContextFactory
                    .ConfigureOptions(optionsBuilder => 
                    {
                        optionsBuilder
                            .UseNpgsql("connection-string");
                    })
                    .UseSchema("oscillation")
                    .UsePrefix("Oscillation");
            });
    });
```
where `UseSchema` and `UsePrefix` are optional.

For `IDbContextFactory` backed one, configure as follows:
```csharp
oscillation
    .UseEntityFrameworkCoreSignalStore(signalStoreBuilder => 
    {
        signalStoreBuilder
            .UseEntityFrameworkFactory<TSignalStoreDbContext>();
    });
```
where `TSignalStoreDbContext` is a `DbContext` whose `IDbContextFactory` is registered via `AddDbContextFactory`.

Both are the same, choose which you familiar with, or write your own signal store to meet your application's needs.

### In-memory notification
The `Oscillation.Notification.InMemory` library provides some extension methods to configure in-memory notification mechanism for both client hosting and server hosting.
Use this library where you want to host signal distributors to the same application you submit signals.

To enable this capability, configure as follows:
```csharp
services.AddInMemorySignalNotificationCenter();

services.AddOscillationClient(oscillation => 
{
    oscillation
        .UseInMemoryNotification();
    // other configurations
});

services.AddOscillationServer(oscillation => 
{
    oscillation
        .UseInMemoryNotification();
    // other configurations
});
```

### NATS notification
The `Oscillation.Notification.Nats.Client` library provides an extension method for client hosting, while the `Oscillation.Notification.Nats.Server` library provides an extension method for server hosting, which are configured as follows:
```csharp
oscillation
    .UseNatsNotification((provider, notificationBuilder) => 
    {
        notificationBuilder
            .UseNatsClient(...)
            .UseSubject(...);
    });
```
NATS is lightweight and low-latency, which is extremely suitable for this workload, we recommend using this configuration for most scenarios.

### PostgreSQL notification
The `Oscillation.Notification.Postgres.Client` library provides an extension method for client hosting, while the `Oscillation.Notification.Postgres.Server` library provides an extension method for server hosting, which are configured as follows:
```csharp
oscillation
    .UsePostgresNotification((provider, notificationBuilder) => 
    {
        notificationBuilder
            .UseConnectionString(...)
            .UseChannel(...);
    });
```
This notification mechanism leverages PostgreSQL `NOTIFY/LISTEN` commands, which is suitable in scenarios where you don't want to use external broker and want the capacity of distributed scheduling.