# Upstream Import and Rename Record

2026-11-24 · Applies to step 1 and step 2 of `docs/opengauss-connector-m1.md`

This file records the fixed baseline for importing the official ADO.NET snapshot into this repository, the path mapping, the mechanical rename rules, the recorded deviations from upstream, and the checkable results of each import. When appending upstream files later, add rows to the tables in the corresponding sections.

## Fixed Baseline

| Item | Value |
| --- | --- |
| Upstream repository | `gitcode.com/opengauss/openGauss-connector-adonet` |
| Local checkout | `D:\Code\dotnet-source\openGauss-connector-adonet` (relative to this repository: `../../dotnet-source/openGauss-connector-adonet`) |
| Commit | `ba5e8ba629b0d660d095da6d8af94f487a00730c` (2023-11-07) |
| Import scope | all seven projects under `src/` plus `src/Shared`, the six projects under `test/`, `LICENSE`, `openGauss.png`, `.editorconfig`, `.gitattributes` |
| D18 | openGauss server version identification in `OpenGaussConnectionStringBuilder.cs`, `Internal/OpenGaussDatabaseInfo.cs` and `PostgresDatabaseInfo.cs`, plus the new `Internal/OpenGaussServerVersion.cs` | The upstream snapshot only exposes the raw `server_version`. This port adds the `Server Version` setting, parses the openGauss product marker from `SELECT version()`, probes `SHOW sql_compatibility` only when the marker is present, and fails a physical connection whose configured version contradicts the server |
| D19 | `Logging/OpenGaussLogManager.cs` gains `TrySetProvider` | Upstream resolves the provider exactly once and forbids replacing it. The DI bridge is installed when a connection or data source is resolved, so a later install has to be possible; an application assignment still takes precedence |
| Not imported | upstream `OpenGauss.sln` (replaced by `OpenGaussConnector.slnx`), `README.md`/`README.en.md` (to be added when the root documentation is rewritten for this project), `OpenGauss.snk` (replaced by this project's key), upstream root `Directory.Build.props`/`Directory.Packages.props`/`global.json` (rebuilt for this project's solution) |

## Path Mapping

| Upstream | This project |
| --- | --- |
| `src/OpenGauss.NET/` | `src/OpenGaussConnector/` |
| `src/OpenGauss.SourceGenerators/` | `src/OpenGaussConnector.SourceGenerators/` |
| `src/OpenGauss.GeoJSON.NET/` | `src/OpenGaussConnector.GeoJSON/` |
| `src/OpenGauss.Json.NET/` | `src/OpenGaussConnector.Json.NET/` |
| `src/OpenGauss.NetTopologySuite/` | `src/OpenGaussConnector.NetTopologySuite/` |
| `src/OpenGauss.NodaTime.NET/` | `src/OpenGaussConnector.NodaTime/` |
| `src/OpenGauss.OpenTelemetry.NET/` | `src/OpenGaussConnector.OpenTelemetry/` |
| `src/Shared/` | `src/Shared/` (kept as is, compiled into each product project by `src/Directory.Build.props`) |
| `test/` | `tests/` |
| `test/OpenGauss.Tests/` | `tests/OpenGaussConnector.Tests/` |
| `test/OpenGauss.PluginTests/` | `tests/OpenGaussConnector.PluginTests/` |
| `test/OpenGauss.NodaTime.Tests/` | `tests/OpenGaussConnector.NodaTime.Tests/` |
| `test/OpenGauss.Specification.Tests/` | `tests/OpenGaussConnector.Specification.Tests/` |
| `test/OpenGauss.TrimmingTests/` | `tests/OpenGaussConnector.TrimmingTests/` |
| `test/OpenGauss.Benchmarks/` | `tests/OpenGaussConnector.Benchmarks/` |

## Mechanical Rename

Perform the global replacements in the following order (longer strings first, so prefixes do not swallow each other). Only identifiers, namespaces, project references and document names are changed; protocols, SQL and public type names are left untouched.

| Upstream | This project |
| --- | --- |
| `OpenGauss.SourceGenerators` | `OpenGaussConnector.SourceGenerators` |
| `OpenGauss.GeoJSON.NET` | `OpenGaussConnector.GeoJSON` |
| `OpenGauss.Json.NET` | `OpenGaussConnector.Json.NET` |
| `OpenGauss.NetTopologySuite` | `OpenGaussConnector.NetTopologySuite` |
| `OpenGauss.NodaTime.NET` | `OpenGaussConnector.NodaTime` |
| `OpenGauss.OpenTelemetry.NET` | `OpenGaussConnector.OpenTelemetry` |
| `OpenGauss.Tests` and the other four test project names | `OpenGaussConnector.*` |
| `OpenGauss.NET` (namespace, assembly, project references, documentation) | `OpenGaussConnector` |

Public type names kept unchanged: `OpenGaussConnection`, `OpenGaussCommand`, `OpenGaussDataReader`, `OpenGaussTransaction`, `OpenGaussParameter`, `OpenGaussParameterCollection`, `OpenGaussConnectionStringBuilder`, `OpenGaussBatch`, `OpenGaussFactory`, `PostgresException`, `OpenGaussDbType`, and so on.

## Recorded Deviations from Upstream

Besides renaming, the import stage made the following changes. Each item gives the reason and a location that can be reviewed, so it can be checked item by item against the upstream snapshot.

| ID | Change | Reason |
| --- | --- | --- |
| D01 | The internal class `OpenGaussConnector` (formerly `OpenGauss.NET.Internal`) is renamed `PhysicalConnector`, and the files are renamed accordingly to `Internal/PhysicalConnector.cs`, `.Auth.cs`, `.FrontendMessages.cs` | The root namespace is `OpenGaussConnector`, so a public type with the same name inside `namespace OpenGaussConnector` resolves as the namespace (CS0118) and the original name cannot be kept. The type lives in the `Internal` namespace and is not an ADO.NET public entry point |
| D02 | Removed `using OpenGauss;` from every file | The upstream root namespace was the dotted `OpenGauss.NET`, so `using OpenGauss;` actually imported its parent namespace and was always redundant; once the root namespace is a single segment, that directive no longer resolves (CS0246) |
| D03 | In `OpenGaussConnectionStringBuilder.cs`, `NET.TargetSessionAttributes` is changed to `OpenGaussConnector.TargetSessionAttributes` | Upstream relied on the implicit resolution that "`NET` is a member namespace of the parent namespace `OpenGauss`"; that no longer holds under a single-segment root namespace |
| D04 | In `Properties/AssemblyInfo.cs`, removed `[assembly: AllowPartiallyTrustedCallers]` and `[assembly: SecurityRules(SecurityRuleSet.Level1)]`; moved `InternalsVisibleTo` into `InternalsVisibleTo` items in the project file | The first two are .NET Framework CAS semantics and do not apply to the five target groups; friend assemblies now use the project public key and are generated with the signing identity |
| D05 | Strong naming now uses this project's `OpenGaussConnector.snk`, with the public key passed to `InternalsVisibleTo` as `$(OpenGaussConnectorPublicKey)` | The design requirement "signing uses the project's own identity" rules out reusing the upstream key |
| D06 | In `PhysicalConnector.Auth.cs`, `Hi` uses `Rfc2898DeriveBytes.Pbkdf2(password, salt, iteration, HashAlgorithmName.SHA1, 32)` on `NET8_0_OR_GREATER` | The old constructor is marked obsolete in .NET 10 (SYSLIB0060). The algorithm and parameters match the original implementation (PBKDF2-HMAC-SHA1, 32 bytes), so authentication behavior is unchanged |
| D07 | In `PhysicalConnector.cs`, on `NET9_0_OR_GREATER`, the obsolete `X509Certificate2` constructors (SYSLIB0057) are replaced by `X509CertificateLoader.LoadCertificateFromFile`/`LoadCertificate`/`LoadPkcs12`/`LoadPkcs12FromFile` | The old constructors are obsolete since .NET 9; the loader is the official replacement path, and certificate selection, trust chain and PEM/PKCS#12 semantics stay unchanged |
| D08 | The binary serialization members of `OpenGaussException`/`PostgresException` are locally suppressed with `#pragma warning disable SYSLIB0051` (and `CS0672`) | Keeps the public API identical across the five asset groups and keeps serialization support on netstandard; the members themselves are obsolete on .NET 8+ but still usable |
| D09 | Removed the `Microsoft.CodeAnalysis.Analyzers` reference and the two empty `AnalyzerReleases.*.md` files from the source generator project; upgraded `Microsoft.CodeAnalysis.CSharp` to 4.8.0 and `Scriban.Signed` to 7.6.1 | The generator is an internal build project and is not published as a distributable analyzer package (RS1036/RS2008 apply only to the latter); Scriban 5.0.0 has known vulnerability warnings (NU1902/1903/1904) |
| D10 | In `TypeHandlerSourceGenerator.cs`, added a null check for `UsingDirectiveSyntax.Name` | Nullable annotations in newer Roslyn make the original form trigger CS8602 (this project enables `TreatWarningsAsErrors`) |
| D11 | `global.json` is not set | Per the project owner's request, the SDK version is not pinned and the installed SDK is used directly (10.0.403 for this verification). The related items in the design document are adjusted along with this item |
| D12 | BCL supplementary package versions as ranges: `System.Text.Json`, `System.Threading.Channels`, `System.Collections.Immutable`, `System.Diagnostics.DiagnosticSource` at `[10.0.0,)`, `System.Runtime.CompilerServices.Unsafe` at `[6.1.2,)`, `Microsoft.Bcl.HashCode` at `[6.0.0,)` | The upstream 6.0.0 combination triggers NU1903 (System.Text.Json known vulnerability warning). The floors match the .NET 10 host used in this verification, the open ranges let a consumer take newer patch releases, and both netstandard assets can still be restored |
| D13 | Removed the upstream `Microsoft.CodeAnalysis.PublicApiAnalyzers` reference and the `PublicAPI.*.txt` declaration files from the four extension projects | The declaration files were generated on the assumption that "nullable annotations are enabled", but the `netstandard2.0` asset disables nullability (the BCL is not annotated), so one set of declarations cannot validate across the five TFM groups (RS0016/RS0017). Public API compatibility is instead covered by package validation and tests at the release stage |
| D14 | Fixed qualified names damaged by the rename: `using X = PhysicalConnector.StatementType`, `PhysicalConnector.Internal.*`, `PhysicalConnector.Json.NET`, `NET.Schema.*` and the like restored to `OpenGaussConnector.*`; every `NET.<Type/namespace>` parent-namespace form other than `using OpenGauss;` changed to the full namespace | The global replacement works on tokens and also hits the root namespace in "namespace-qualified" forms; such places occur in namespace aliases, base class references, generic parameters and comments. Found and fixed by the file-by-file comparison in `eng/Verify-UpstreamFidelity.ps1` |
| D15 | Authors, company, copyright and license are centralized in the root `Directory.Build.props`, and `<Authors>` is removed from each project file (upstream was `Microestc`) | Common package metadata should be defined in only one place; each project keeps only package-specific information such as package description and tags |
| D16 | The test connection string environment variable is renamed to `OPENGAUSS_TEST_DB`, while keeping `NPGSQL_TEST_DB` as a fallback | The driver no longer uses the `Npgsql` name; the fallback keeps upstream CI scripts and documentation working |
| D17 | The `AppContext` switch names in obsolete-behavior notices and test assertions are changed to `OpenGaussConnector.EnableLegacyTimestampBehavior`, `OpenGaussConnector.DisableDateTimeInfinityConversions` | The code actually reads `OpenGaussConnector.*`; the original notices pointed to a key that nobody reads, so configuring per the notice had no effect |

## Import Verification

| Date | Check | Result |
| --- | --- | --- |
| 2026-11-24 | `dotnet restore` + `dotnet build OpenGaussConnector.slnx -c Release` | Succeeded, 0 warnings 0 errors, produced five `OpenGaussConnector.dll` files for `netstandard2.0`, `netstandard2.1`, `net8.0`, `net9.0`, `net10.0` |
| 2026-11-24 | Generator output (`-p:EmitGeneratedFiles=true`) | Generated `OpenGaussConnectionStringBuilder.Generated.cs` and 43 type handler partial classes, all in namespace `OpenGaussConnector` |
| 2026-11-24 | Offline smoke test (`artifacts/tmp/smoke`, not committed) | Connection string defaults/round-trip/keyword case-insensitivity, `OpenGaussFactory.Instance`, `DbProviderFactories.GetFactory("OpenGaussConnector")`, closed-connection state, parameter collection, `DbBatch` and the namespace assertions all passed |
| 2026-11-24 | `eng/Verify-UpstreamFidelity.ps1` | File-by-file comparison of 424 upstream `.cs` files; no unexpected differences besides the 10 recorded deviations in the table above; the script is used for regression checks after upstream files are appended |
| 2026-11-24 | Migration of the six test projects (`tests/`, all `net10.0`, versions via CPM) | All compile; `eng/Test-Offline.ps1` ran through 190 cases that need no server (DI 15, extensions 8, upstream offline fixtures 167) |
| Pending | Forced loading and package consumption of the five asset groups | See [development progress](opengauss-connector-progress.md) |

`eng/Verify-UpstreamFidelity.ps1` folds both sides into the same set of placeholder names (project/namespace → `NS.*`, internal connection type → `CONNTYPE`) before comparing the contents, so any change other than renaming is reported. When the local upstream snapshot is missing, first check out the corresponding commit following the path in [references](references.md#local-source-checkout).
