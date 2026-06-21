# EventHandlers — Domain Event Handlers

This project contains MediatR **notification handlers** that react to domain events.

## Pattern

```csharp
// 1. Define the notification (domain event) — usually in Domain or CommandHandlers:
public sealed record UserRegisteredEvent(string UserId, string Email) : INotification;

// 2. Handle it here:
public sealed class SendWelcomeEmailOnRegistration : INotificationHandler<UserRegisteredEvent>
{
    public Task Handle(UserRegisteredEvent notification, CancellationToken ct)
    {
        // side-effect: send email, update audit log, emit to message bus, etc.
        throw new NotImplementedException();
    }
}
```

## Notes

- Handlers must be **idempotent** where possible (events can be retried on failure).
- Multiple handlers for the same event run sequentially by default in MediatR;  
  use `INotificationHandler<T>` for in-process fan-out.
- For durability (at-least-once delivery across process restarts), consider  
  a transactional outbox pattern in a future phase.
- Registration: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SomeHandler>())`  
  — already included if scanned alongside CommandHandlers.
