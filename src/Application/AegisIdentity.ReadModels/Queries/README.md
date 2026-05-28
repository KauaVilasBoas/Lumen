# ReadModels — Query Handlers

This project contains CQRS **query handlers**: they read state and never mutate it.

## Pattern

```csharp
public sealed class GetUserProfileQueryHandler
    : IRequestHandler<GetUserProfileQueryHandler.Query, GetUserProfileQueryHandler.Result>
{
    /// <summary>Query input — filtering/pagination parameters.</summary>
    public sealed record Query(string UserId) : IRequest<Result>;

    /// <summary>Read model returned to the caller.</summary>
    public sealed record Result(string Id, string Email, string Username);

    public async Task<Result> Handle(Query query, CancellationToken ct)
    {
        // Read from the read-side store; never write.
        throw new NotImplementedException();
    }
}
```

## Notes

- Handlers in this project are **pure reads** — no `UpdateAsync`, `InsertAsync`, or domain events.
- The read side may eventually have its own optimised projection store (e.g., a flat MongoDB view).  
  For now it shares the same repositories as the write side.
- Registration: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SomeQueryHandler>())`  
  — already included if scanned alongside CommandHandlers.
