# openGauss ADO.NET Driver Development Progress

Updated 2026-11-24

## Current State

| Item | Status | Evidence or pending |
| --- | --- | --- |
| Upstream research | Done for this round | Pinned the ADO.NET and OB reference commits, checked the four JDBC release tags and the local development line, openGauss release materials and DI package metadata. [References](references.md) |
| Design, feature matrix | Reviewed and updated | Project list and functional acceptance for the first seven packages, five TFMs and five upstream extensions; JDBC version differences and in-driver server version selection. [Driver design](opengauss-connector-design.md) · [Feature matrix](opengauss-connector-feature-matrix.md) |
| M1 kickoff checklist | Completed | Records the execution order for upstream import, first build, test entry points and package consumption. [M1 kickoff checklist](opengauss-connector-m1.md) |
| Upstream import and rename | Done | Pinned the `ba5e8ba` snapshot; seven product projects and six test projects imported under new paths; mechanical renames and 13 explicit deviations recorded item by item. [Import record](opengauss-connector-import.md) |
| Core source and source generators | Done (five targets compile) | The five assets of `OpenGaussConnector` build successfully, 0 warnings 0 errors; the connection string generator and 43 type handler partial classes generate normally. |
| Core DataSource/Builder and DI package | Done (five targets compile) | `OpenGaussDataSource`/`OpenGaussDataSourceBuilder` provided per .NET 8/9/10 and holding an independent connection source; the DI package compiles for the five assets, 15 registration, lifetime and disposal tests pass. |
| Five upstream extensions | Done (five targets compile) | Each of the five packages produces five assets, 0 warnings 0 errors; `tests/OpenGaussConnector.Extensions.Tests` 8 pass (plugin registration and OpenTelemetry subscription). The upstream plugins' missing object-level write members were added to `OpenGaussSimpleTypeHandler` following Npgsql semantics. |
| Test project integration and offline tests | Done | Six test projects moved to `net10.0` + CPM and wired into the SLNX, all compile. `eng/Test-Offline.ps1` runs 227 cases that need no server with a fixed fixture whitelist; cases that need a server are driven by `OPENGAUSS_TEST_DB`. |
| Five assets and package consumption | Done | `dotnet pack` produces the seven packages and their symbol packages for `0.1.0`. `eng/Verify-Packages.ps1` inspects each one (five `lib/<TFM>/` assets with XML docs, license, icon, README, dependency ranges, symbol files), then restores, builds and runs throwaway consumers from the produced folder: `netstandard2.0` and `netstandard2.1` libraries resolve their own asset, `net8.0`/`net9.0`/`net10.0` console programs exercise the factory, connection string builder, data source, DI registrations, the four type-mapping plugins and the OpenTelemetry subscription, a `netstandard2.0` class library binds on a `net10.0` host, and a `net48` console program runs on the Standard asset. |
| Server version identification | Done (offline) | `Server Version` connection string setting (`Auto` by default, or e.g. `6.0.6`); the openGauss product version is parsed from `SELECT version()`, `SHOW sql_compatibility` is probed only when the marker is present, and a contradicting configuration fails the physical connection. `OpenGaussConnection.OpenGaussVersion`/`LongServerVersion`/`CompatibilityMode` expose the result; 29 offline tests cover parsing, comparison and the connection string round trip. |
| DI logging integration | Done (offline) | `AddOpenGaussLogging()` bridges the driver's logging abstraction to the container's `ILoggerFactory` (category, level, connection id); the bridge resolves categories lazily and stays silent after the factory is disposed. 8 offline tests. |
| Trimming and Native AOT (B07) | Measured, not supported | `dotnet publish tests/OpenGaussConnector.TrimmingTests -r win-x64 --self-contained true` fails trim analysis with IL2026, IL2060, IL2070, IL2087 and IL2093 in 8 files: `JsonHandler` uses the reflection-based `JsonSerializer` overloads, `ArrayHandler`/`NullableHandler` call `MakeGenericMethod`, `ConnectorTypeMapper` reflects over `ImplementedInterfaces`, `UnmappedEnumHandler`/`OpenGaussRange` pass unannotated type arguments, and `GetFieldType` disagrees with the base annotation. The legacy project had the same publish settings and no suppressions, so the gap is inherited. Closing it needs `JsonTypeInfo`/`JsonSerializerContext` support plus annotations validated against a live server, so it is an M4 item. |
| Batch usage from a Standard-compiled library | Known limitation, recorded | Virtual batch members resolve through the netstandard `DbBatch` shim, so a library compiled against the Standard asset that calls a batch fails on .NET 6+ with `TypeLoadException`; non-batch APIs bind normally. A library that needs batches must multi-target or keep the calls in the application assembly. [M1 checklist](opengauss-connector-m1.md) |
| Server live runs | Not started | No package has been exercised against a real server; the M2 environments are still to be established. |
| Actual deployment version and upgrade status | To confirm | Checked public materials for 6.0.6 and 7.0.0 LTS. The user environment's version, release form and upgrade situation are to be collected. |
| Development machine SDK and runtime | Inventoried | Detected 10.0.403 SDK and .NET 8/9/10 runtimes; the repository sets no `global.json` and uses the installed SDK directly. The exact versions used by CI are to be recorded. |

## Milestones

| Stage | Status | Work and exit criteria |
| --- | --- | --- |
| M0 · Establish design baseline | Done this round | Record upstream snapshot, versioned materials, scope and capability acceptance rules. |
| M1 · Core, DI and five upstream extension ports | Done | Core, generator, DataSource, DI, the five extension packages, test project integration, packing and independent consumption verification are done: seven packages, five assets each, consumers on all five TFM sets. [M1 kickoff checklist](opengauss-connector-m1.md) |
| M2 · 6.0.6 LTS core live runs | Not started | Verify P0 items C01, C02, C05, C07, C08, C13 and E01–E03, E10, T01–T03, T06, T07. Record results per A, B, PG. Complete the five-asset live smoke test in one verified mode, and create a connection through the DI path. |
| M3 · 7.0.0 LTS and cross-version regression | Not started | Repeat the M2 core cases and the five-asset smoke test, complete per-package functional acceptance for the X01–X05 supported environments. Check catalog, compatibility mode, extension and type differences. Test version conflicts, unknown versions, mixed nodes and physical connection rebuild after upgrade. |
| M4 · Priority features and pre-release | Not started | Advance P1 items such as mixed authentication, SM3, primary/standby, batching, COPY, procedures and extension metadata per test results. Validate the consumption reports of the seven packages and the corresponding environment capabilities, and record known limitations and skip reasons. |
| M5 · Specialized enhancements | To assess | Track the oGRAC, ATF, TLCP, fully encrypted and replication output plugins separately. |

## M1 Staged Delivery Record

Each stage closes with "build → test → review → update this file → commit". The stage order corresponds to steps 1–5 of the [M1 kickoff checklist](opengauss-connector-m1.md).

| Stage | Scope | Status | Evidence |
| --- | --- | --- | --- |
| A | Repository skeleton (`slnx`, CPM, build properties) and upstream core import | Done | 465 imported files; `dotnet build OpenGaussConnector.slnx -c Release` succeeds, the five assets 0 warnings 0 errors; generator output checked; offline smoke test passes. [Import record](opengauss-connector-import.md) |
| B | Core DataSource/Builder and `OpenGaussConnector.DependencyInjection` | Done | Five assets 0 warnings 0 errors; `tests/OpenGaussConnector.DependencyInjection.Tests` 15 pass (transient connection, alias precedence, duplicate registration, singleton disposal, keyed isolation, data source connection string fixed). Logging factory integration is recorded as a follow-up M1 item per the [driver design](opengauss-connector-design.md). |
| C | `OpenGaussConnector.Json.NET` | Done | Five assets 0 warnings 0 errors; `UseJsonNet` registers `JsonNetTypeHandlerResolverFactory`, covered by tests. Dependency declared as the range `Newtonsoft.Json [13.0.4,14.0.0)`. |
| D | `OpenGaussConnector.NodaTime` | Done | Five assets 0 warnings 0 errors; `UseNodaTime` registers `NodaTimeTypeHandlerResolverFactory`, dependency range `NodaTime [3.3.7,4.0.0)`. The upstream `net6.0` target changed to the five TFMs. |
| E | `OpenGaussConnector.NetTopologySuite` | Done | Five assets 0 warnings 0 errors; `UseNetTopologySuite` registers `NetTopologySuiteTypeHandlerResolverFactory`; object-level write members added. |
| F | `OpenGaussConnector.GeoJSON` | Done | Five assets 0 warnings 0 errors; `UseGeoJson` registers `GeoJSONTypeHandlerResolverFactory`; object-level write members added. Dependency range `GeoJSON.Net [1.4.1,2.0.0)`, raised from 1.2.19 to shed a vulnerable transitive dependency. |
| G | `OpenGaussConnector.OpenTelemetry` | Done | Five assets 0 warnings 0 errors; `AddOpenGauss` subscribes to the `OpenGauss` source, and a capturing processor verifies that only that source's activities are collected. Dependency range `OpenTelemetry [1.20.1,2.0.0)`; 1.21.0 pulls `Microsoft.Extensions.*` 11.0.0, which this repository does not target. |
| H | Upstream test project integration and offline test entry point | Done | Six test projects compile (`net10.0`, CPM, project reference fixes); new `eng/Test-Offline.ps1`: DI 23, extensions 8, upstream offline fixture 196, 227 total pass. The test connection string now uses `OPENGAUSS_TEST_DB`, with `NPGSQL_TEST_DB` kept as fallback. |
| I | Packing, `eng/Verify-Packages.ps1` and independent consumption verification | Done | Seven `.nupkg` plus seven `.snupkg` at `0.1.0`, 0 warnings 0 errors. Each package inspected for five `lib/<TFM>/` assemblies and XML docs, license, icon, README, symbol files and dependency ranges; `netstandard2.0`/`netstandard2.1` consumers resolve their own asset, and `net8.0`/`net9.0`/`net10.0` programs load all seven assemblies at `0.1.0.0` from the application directory and exercise core, data source, DI, the four type-mapping plugins and tracing. Three packages gained a README, and every packaged dependency is now a NuGet range. The consumer set also measures a `netstandard2.0` class library on a `net10.0` host and a `net48` console program on the Standard asset. |

### Order for live-server runs

1. Prepare single-node environments for 6.0.6 and 7.0.0, and record the release form, build number, database modes that can be created, test account privileges and authentication configuration.
2. For both server versions first do startup, SHA256, basic SQL and transaction smoke tests. Record the product version, startup parameters and actual compatibility mode identified by the driver, then extend to parameters, types, version-related catalog, pool, cancellation and TLS.
3. Establish a mapping table between compatibility modes and feature cases. For failing items, reproduce with the fixed-version JDBC, legacy ADO.NET or server-side SQL, choosing the comparison per scenario.
4. Establish the environments needed for the 6.0.6/7.0.0 primary/standby and TLS specialty items, arranged per actual deployment conditions. For what to advance, see the [feature matrix](opengauss-connector-feature-matrix.md).
5. In both environments check PostGIS and extension type availability, and record the actual extension versions. Run the applicable GIS, Json.NET, NodaTime and OpenTelemetry end-to-end tests, and record the packages and assets consumed.

## Acceptance Log

Record one row per verification, by feature and target combination. Record driver version identification cases separately with explicit configuration, probed values, capability availability and the compatibility path taken. Record specific gaps when the environment is missing. After a case runs, annotate the result and reproduction location; redact outputs that involve credentials.

Offline results go into the staged delivery record above; the table below records only cases that need a real server.

| Date | Feature ID | Driver commit or package version | Server and build number | Database mode and extensions | OS / host / asset TFM | Auth / TLS / topology | Result | Log or case path |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| To run | C01, C02 | To establish | 6.0.6 LTS, build number to record | A, extensions to record | Windows or Linux / .NET 10 / `net10.0` | SHA256 / to record / single node | To run | To record |
