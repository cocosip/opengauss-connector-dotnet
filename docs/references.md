# References

The source verification date is 2026-11-24. The checkout paths below are based on this repository's root directory. Source file paths in the documentation are based on the root directory of the corresponding reference repository.

## Local source checkout

| ID | Repository and relative path | Verification baseline | Main entry points |
| --- | --- | --- | --- |
| ADO | `../../dotnet-source/openGauss-connector-adonet` | `ba5e8ba629b0d660d095da6d8af94f487a00730c`, 2023-11-07 | `README.md`, `LICENSE`, `Directory.Build.props`, `src/OpenGauss.NET/`, `src/OpenGauss.SourceGenerators/`, `test/OpenGauss.Tests/` |
| JDBC | `../../java-source/openGauss-connector-jdbc` | local `master` is `f2b803a661d4e5e42464ca57377e10c790922c78`, 2026-11-09. The release tags correspond to commits `v5.0.3` (`9283fdf`), `v6.0.0` (`9deecee`), `v6.0.6` (`f7ee5a4`), `v7.0.0` (`6e51e28`) | `pgjdbc/src/main/java/org/postgresql/`, `pgjdbc/src/test/java/org/postgresql/`, `pgjdbc/src/test/java/org/ograc/`, `README_cn.md`, `LICENSE`, `Third_Party_Open_Source_Software_Notice` |
| OB | `../obconnector-dotnet` | `903e103298ee59ef0ee9c3b08a3337952d5e2c60`, 2026-11-23 | `OceanBaseConnector.slnx`, `Directory.Build.props`, `Directory.Packages.props`, `src/OceanBaseConnector/`, `src/OceanBaseConnector.DependencyInjection/`, `tests/OceanBaseConnector.ConsumerTests.NetStandard/`, `eng/Verify-Packages.ps1` |

### Official ADO.NET project inventory

| Upstream project or directory | Verification entry point |
| --- | --- |
| `src/OpenGauss.NET/` | Core library. For version capability, pooling, and invocation semantics, refer to `Internal/OpenGaussDatabaseInfo.cs`, `OpenGaussDatabaseInfoCacheKey.cs`, `OpenGaussCommand.cs`; for tracing, refer to `OpenGaussActivitySource.cs` |
| `src/OpenGauss.GeoJSON.NET/` | Project file, `OpenGaussGeoJSONExtensions.cs`, `GeoJSONOptions.cs`, `Internal/GeoJSONHandler.cs`, with test cases in `test/OpenGauss.PluginTests/GeoJSONTests.cs` |
| `src/OpenGauss.Json.NET/` | Project file, `OpenGaussJsonNetExtensions.cs`, with test cases in `test/OpenGauss.PluginTests/JsonNetTests.cs` |
| `src/OpenGauss.NetTopologySuite/` | Project file, `OpenGaussNetTopologySuiteExtensions.cs`, with test cases in `test/OpenGauss.PluginTests/NetTopologySuiteTests.cs` |
| `src/OpenGauss.NodaTime.NET/` | Project file, `OpenGaussNodaTimeExtensions.cs`, with test cases in `test/OpenGauss.NodaTime.Tests/` |
| `src/OpenGauss.OpenTelemetry.NET/` | Project file, `TracerProviderBuilderExtensions.cs` and `README.md` |
| `src/OpenGauss.SourceGenerators/` | Internal generator; the project file declares `IsPackable=false` |
| `test/` | Five test projects — core, plugins, NodaTime, ADO.NET specification, and trimming — plus the benchmark project. The old `OpenGauss.sln` loads only the core tests. The plugin, NodaTime, specification, trimming, and benchmark projects contain old project references. GIS test cases are controlled by conditional compilation of `TestGeoJSONTests` and `NetTopologySuiteTest`. |

In ADO.NET's `Directory.Build.props`, `VersionPrefix` is `6.0.3`, which indicates the upstream build version. `pgjdbc/pom.xml` on JDBC's local `master` is marked 8.0.0-RC1 and is used as a development-line clue. Release content is evidenced by the corresponding tags. The branch histories of 6.0.6 and 7.0.0 have their own commits; diffs between tag trees are used to verify how much code was retained.

```powershell
git -C ../../dotnet-source/openGauss-connector-adonet show ba5e8ba:src/OpenGauss.NET/Internal/ProtocolVersion.cs
git -C ../../dotnet-source/openGauss-connector-adonet ls-tree -r --name-only ba5e8ba -- src test
git -C ../../java-source/openGauss-connector-jdbc show v5.0.3:pgjdbc/src/main/java/org/postgresql/PGProperty.java
git -C ../../java-source/openGauss-connector-jdbc diff v6.0.0 v6.0.6 -- pgjdbc/src/main/java/org/postgresql/PGProperty.java pgjdbc/src/main/java/org/postgresql/jdbc/PgDatabaseMetaData.java
git -C ../../java-source/openGauss-connector-jdbc show v6.0.6:pgjdbc/src/main/java/org/postgresql/core/v3/ConnectionFactoryImpl.java
git -C ../../java-source/openGauss-connector-jdbc diff v6.0.6 v7.0.0 -- pgjdbc/src/main/java/org/postgresql/PGProperty.java pgjdbc/src/main/java/org/postgresql/core/v3/ConnectionFactoryImpl.java
git -C ../../java-source/openGauss-connector-jdbc diff --name-status v7.0.0 f2b803a -- pgjdbc/src/main/java pgjdbc/src/test/java/org/ograc
git -C ../obconnector-dotnet show 903e103:Directory.Packages.props
git -C ../obconnector-dotnet show 903e103:src/OceanBaseConnector.DependencyInjection/OceanBaseServiceCollectionExtensions.cs
```

| JDBC comparison | Representative commits and files |
| --- | --- |
| `v6.0.0` to `v6.0.6` | `77acc2b` reconnection configuration, `dfd3ca7` prepared state after reconnection, `22a6b2e` generated columns, `e75486e` old-server `bit_output` compatibility |
| Release differences between `v6.0.6` and `v7.0.0` | `e4ef598` oGRAC, `8c8ea6b` ATF recovery flow, `d2950de` multi-vector query, `08a89fc` streaming buffer limits, `5caf8f5` COPY receive limits |
| `v7.0.0` to the local development line | new test cases in `pgjdbc/src/test/java/org/ograc/`, changes to `jdbc/ORResultSet.java` |

## openGauss official material

| ID | Material | Purpose |
| --- | --- | --- |
| OG1 | [openGauss community downloads](https://opengauss.org/zh/download/) | 6.0.6 LTS, 7.0.0 LTS, and community distributions |
| OG2 | [openGauss 7.0.0 LTS release notes](https://docs.opengauss.org/zh/docs/7.0.0/release_notes/release_notes.html) | New enterprise edition features, compatibility changes, and product capabilities |
| OG3 | [JDBC connection parameters, 6.0.0 documentation](https://docs.opengauss.org/en/docs/6.0.0/docs/DeveloperGuide/connecting-to-a-database-JDBC.html) | Primary/standby selection, connection options, fetch size, batch rewriting, fully-encrypted entry points |
| OG4 | [JDBC SSL, 6.0.0 documentation](https://docs.opengauss.org/en/docs/6.0.0/docs/DeveloperGuide/connecting-to-a-database-using-ssl-jdbc.html) | SSL modes, certificate configuration |
| OG5 | [Client access authentication](https://docs.opengauss.org/en/docs/6.0.0/docs/DatabaseAdministrationGuide/client-access-authentication.html), [Security and authentication parameters](https://docs.opengauss.org/en/docs/6.0.0/docs/DatabaseReference/security-and-authentication_postgresql-conf.html) | `pg_hba.conf`, `password_encryption_type`, SHA256 and SM3 |
| OG6 | [CREATE DATABASE, 6.0.0 documentation](https://docs.opengauss.org/en/docs/6.0.0/docs/SQLReference/create-database.html) | `DBCOMPATIBILITY` and database creation parameters |
| OG7 | [COPY, 6.0.0 documentation](https://docs.opengauss.org/en/docs/6.0.0/docs/SQLReference/copy.html) | STDIN/STDOUT and text, CSV, binary options |
| OG8 | [Logical decoding, 6.0.0 documentation](https://docs.opengauss.org/en/docs/6.0.0/docs/DatabaseOMGuide/logical-decoding.html) | Server-side logical decoding entry point |
| OG9 | [oGRAC 7.0.0 LTS release notes](https://docs.opengauss.org/zh/docs/7.0.0/ograc/release_notes/release_notes.html) | Multi-read multi-write, multi-instance deployment, and JDBC entry points |
| OG10 | [Official ADO.NET source](https://gitcode.com/opengauss/openGauss-connector-adonet), [Official JDBC source](https://gitcode.com/opengauss/openGauss-connector-jdbc) | Upstream code and license |
| OG11 | [openGauss 6.0.0 PostGIS support and constraints](https://docs.opengauss.org/en/docs/6.0.0/docs/ExtensionReference/postgis-support-and-constraints.html), [openGauss 7.0.0 PostGIS extension](https://docs.opengauss.org/zh/docs/7.0.0/extension_reference/extension_reference/third_party/postgis.html) | The 6.0.0 documentation lists `geometry` and `geography`. The 7.0.0 documentation lists PostGIS 2.4.2 and installation steps. |
| OG12 | [openGauss 6.0.0 CALL](https://docs.opengauss.org/en/docs/6.0.0/docs/SQLReference/call.html) | Function, procedure, output parameter, and privilege semantics |
| OG13 | [Platform and client compatibility, 6.0.0 documentation](https://docs.opengauss.org/en/docs/6.0.0/docs/DatabaseReference/platform-and-client-compatibility.html) | `sql_compatibility` is a readable internal parameter. The specific release version and mode are subject to actual testing. |

OG3–OG8 come from the 6.0.0 documentation. Test reports should record the tested patch version, distribution form, compatibility mode, extensions, and node topology. The server-side capabilities that 7.0.0 lists in OG2 still require item-by-item verification of client interfaces, types, and protocol.

## .NET and NuGet official material

| ID | Material | Purpose |
| --- | --- | --- |
| MS1 | [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) | `Directory.Packages.props`, per-TFM conditional version declarations, transitive pinning, and pack impact |
| MS2 | [dotnet sln](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln) | `.slnx` management and CLI usage |
| MS3 | [.NET Standard](https://learn.microsoft.com/en-us/dotnet/standard/net-standard) | Standard target framework and consuming host compatibility |
| MS4 | [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | Runtime maintenance windows |
| MS5 | [NuGet V3 Package Base Address](https://learn.microsoft.com/en-us/nuget/api/package-base-address-resource) | Public index queries for package IDs |
| MS6 | [Microsoft DI guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines) | Container ownership, disposal, and lifetime |
| MS7 | [DI Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/8.0.0), [DI container](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/9.0.0), [Logging Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/10.0.0) | The `8.0.0`, `9.0.0`, and `10.0.0` metadata of all three packages — nine groups in total — has been verified through NuGet V3 `.nuspec`. Both DI packages have two Standard dependency groups at 8.0.0, and Logging Abstractions 8.0.0 provides a Standard 2.0 group. |
| MS8 | [.NET Package Validation](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview), [NuGet package compatibility rules](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/nuget-package-compatibility-rules), [DbDataSource](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbdatasource) | Cross-TFM public API compatibility and the modern data source contract |
| MS9 | [DbProviderFactories.RegisterFactory](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbproviderfactories.registerfactory), [.NET Framework factory configuration](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/obtaining-a-dbproviderfactory) | Available targets for the factory registration API and the .NET Framework configuration approach |
| MS10 | [VSTest running tests conditionally](https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests) | NUnit categories, xUnit traits, and `dotnet test --filter` |

On 2026-11-24, both the package details page for the exact ID `OpenGaussConnector` on NuGet.org and the V3 package version index returned HTTP 404. The same version index service returned HTTP 200 for `Newtonsoft.Json`. The design adopts `OpenGaussConnector` as the core package ID. [Package details page](https://www.nuget.org/packages/OpenGaussConnector) · [V3 package version index](https://api.nuget.org/v3-flatcontainer/opengaussconnector/index.json)

On the same day, the V3 package version index for `OpenGaussConnector.DependencyInjection` returned HTTP 404. [DI package version index](https://api.nuget.org/v3-flatcontainer/opengaussconnector.dependencyinjection/index.json)

On the same day, exact V3 queries for the five extension package IDs all returned HTTP 404. [GeoJSON](https://api.nuget.org/v3-flatcontainer/opengaussconnector.geojson/index.json) · [Json.NET](https://api.nuget.org/v3-flatcontainer/opengaussconnector.json.net/index.json) · [NetTopologySuite](https://api.nuget.org/v3-flatcontainer/opengaussconnector.nettopologysuite/index.json) · [NodaTime](https://api.nuget.org/v3-flatcontainer/opengaussconnector.nodatime/index.json) · [OpenTelemetry](https://api.nuget.org/v3-flatcontainer/opengaussconnector.opentelemetry/index.json)

## Extension package specification and upstream material

| ID | Material | Purpose |
| --- | --- | --- |
| EXT1 | [RFC 7946 GeoJSON](https://www.rfc-editor.org/rfc/rfc7946.html) | WGS 84 longitude/latitude, bounding box, and legacy CRS semantics |
| EXT2 | [PostGIS ST_AsEWKB](https://postgis.net/docs/ST_AsEWKB.html), [openGauss PostGIS extension](https://docs.opengauss.org/zh/docs/7.0.0/extension_reference/extension_reference/third_party/postgis.html) | The SRID and geometry dimensions of EWKB, and the installation conditions for the target server |
| EXT3 | [Json.NET Serialization Settings](https://www.newtonsoft.com/json/help/html/SerializationSettings.htm) | Custom settings, `TypeNameHandling`, `ISerializationBinder` |
| EXT4 | [NodaTime Core Types](https://nodatime.org/3.2.x/userguide/core-types) | Mapping boundaries for `Instant`, `Duration`, and `Period` |
| EXT5 | [OpenTelemetry .NET Instrumentation](https://opentelemetry.io/docs/languages/dotnet/instrumentation/) | Emission and subscription of `ActivitySource` |
| EXT6 | [OpenTelemetry database span semantic conventions](https://opentelemetry.io/docs/specs/semconv/database/database-spans/) | Standardized handling of span tags, errors, and query text |

Anchor note: because the heading text was translated, the anchors that other documents link to are now `references.md#local-source-checkout` (for the "Local source checkout" heading) and `references.md#net-and-nuget-official-material` (for the ".NET and NuGet official material" heading).
