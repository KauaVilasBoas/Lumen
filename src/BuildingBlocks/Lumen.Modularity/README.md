# Lumen.Modularity

Building block reutilizável para construir **monolitos modulares** em ASP.NET Core. Marque uma classe com `[Module]` implementando `IModule` e o host descobre o módulo por assembly scanning, registra seus serviços, mapeia seus endpoints e liga o event bus — sem fiação manual.

## Instalação

```bash
dotnet add package Lumen.Modularity
```

> Requer um projeto ASP.NET Core (Web SDK) — a lib usa o shared framework `Microsoft.AspNetCore.App` para mapear endpoints.

## Conceito

- **`[Module]` / `IModule`** — cada módulo é uma vertical autocontida. `IModule` expõe `RegisterServices(IServiceCollection, IConfiguration)` e `MapEndpoints(IEndpointRouteBuilder)`.
- **`AddModules` / `MapModules`** — auto-discovery por assembly scanning: o host registra serviços e mapeia endpoints de todos os módulos sem registro manual.
- **`IEventBus` / `InProcessEventBus` / `AddEventBus`** — barramento in-process para integration events entre módulos. `PublishAsync` abre um DI scope por publicação (handlers `Scoped`, como `DbContext`, funcionam) e resolve todos os `IIntegrationEventHandler<TEvent>`.
- **`IIntegrationEvent` / `IntegrationEvent`** — base para eventos de integração cross-módulo (carrega `EventId` e `OccurredOn`).

## Uso

Defina um módulo:

```csharp
[Module]
public sealed class BillingModule : IModule
{
    public static Assembly Assembly => typeof(BillingModule).Assembly;

    public void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // DI do módulo
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // rotas do módulo
    }
}
```

Componha no host — as únicas chamadas necessárias:

```csharp
builder.Services.AddModules(BillingModule.Assembly, OrdersModule.Assembly);
builder.Services.AddEventBus(BillingModule.Assembly, OrdersModule.Assembly);

var app = builder.Build();
app.MapModules();
```

Comunicação entre módulos via integration event:

```csharp
public sealed record OrderPlaced(Guid OrderId) : IntegrationEvent;

// publica (ex.: no módulo Orders)
await _eventBus.PublishAsync(new OrderPlaced(orderId), ct);

// consome (ex.: no módulo Billing) — handler descoberto pelo AddEventBus
internal sealed class OrderPlacedHandler : IIntegrationEventHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced e, CancellationToken ct = default) { /* ... */ }
}
```

## Princípio

**0 dependências de internals entre módulos.** Cada módulo expõe um assembly de Contratos (interfaces + integration events); outros módulos referenciam só os Contratos, nunca os internals. `Lumen.Modularity` fornece a base para esse padrão.

## Licença

[MIT](https://github.com/KauaVilasBoas/Lumen/blob/main/LICENSE)
