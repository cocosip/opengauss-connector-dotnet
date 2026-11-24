# OpenGaussConnector.DependencyInjection

`Microsoft.Extensions.DependencyInjection` integration for the OpenGaussConnector ADO.NET provider.

```csharp
services.AddOpenGaussDataSource(connectionString); // singleton data source, .NET 8 and later
services.AddOpenGaussConnection(connectionString); // transient connections, all targets
```

- `AddOpenGaussDataSource` registers a singleton `OpenGaussDataSource`, which owns its own connector
  source and therefore its own pool; equal connection strings in separate data sources never share a
  pool. It also registers a `DbDataSource` alias when no other one is registered. Available on
  .NET 8, 9 and 10.
- `AddKeyedOpenGaussDataSource(serviceKey, connectionString)` does the same per service key, so each key
  gets an independent data source and pool that resolve to the same instance through the concrete type
  and through `DbDataSource`.
- `AddOpenGaussConnection` registers transient, closed `OpenGaussConnection` instances owned by the
  resolving scope, and adds the `DbConnection` alias when the base type is free.

`AddOpenGaussLogging()` routes the driver's own log entries into the container's `ILoggerFactory`, keeping the
driver category, level and connection id. The driver's logging entry point is process-wide, so the first installed
provider covers the process and an application that assigns `OpenGaussLogManager.Provider` itself is never
overridden.

Every extension takes an optional `Action<IServiceProvider, OpenGaussDataSourceBuilder>` to adjust the
data source before it is built. Registering the same service twice throws instead of silently replacing
the earlier registration.

The container disposes the data source, which releases the pool. Connections created from a data source
must be disposed by the caller, and their connection string cannot be changed.
