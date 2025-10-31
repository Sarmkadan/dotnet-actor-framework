# Supervision Trees and Restart Strategies

This guide covers how to build and configure supervision hierarchies in the DotNet Actor Framework.

## Supervision Strategies

Supervisors handle the failures of their child actors using specific strategies:

- **Restart**: The child actor is restarted, resetting its state.
- **Stop**: The child actor is stopped permanently.
- **Resume**: The child actor ignores the error and continues processing the next message.
- **Escalate**: The supervisor cannot handle the error and escalates it to its own supervisor.
- **Backoff**: Restarts with an exponential backoff delay to prevent rapid failing loops.

## Configuring a Supervisor

When creating an actor, you can pass a supervisor reference:

```csharp
var supervisor = await system.CreateActorAsync(new ActorPath("/user/supervisor"));
var child = await system.CreateActorAsync(new ActorPath("/user/supervisor/child"), supervisor);
```

## Backoff Policies and Persistence

When an actor crashes, it loses its volatile state. If you have persistence enabled, the actor will reload its state from the snapshot store upon restart. By using a **Backoff** strategy, you ensure the database or external dependency has time to recover before the actor attempts to resume processing.

## Troubleshooting

- **Actor not restarting?**: Check if the max retries limit was hit. The supervisor escalates if the limit is exceeded.
- **Deadlocks during restart?**: Avoid holding locks across asynchronous message boundaries.
