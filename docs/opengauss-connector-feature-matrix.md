# openGauss ADO.NET Feature Matrix

2026-11-24 · Source investigation baseline

ADO refers to the official ADO.NET fixed snapshot. The feature table is used to track the .NET port. The JDBC version difference table gives evidence for `v5.0.3`, `v6.0.0`, `v6.0.6`, `v7.0.0` and the local development line. The ADO core files mentioned in the table are located in `src/OpenGauss.NET/`, and the corresponding JDBC files are located in `pgjdbc/src/main/java/org/postgresql/`. Official document numbers are listed in [references](references.md).

| Priority | Meaning |
| --- | --- |
| P0 | Must be delivered and pass acceptance in the first release |
| P1 | Preferred for porting or enhancement; included in the first release or a later version according to live-server acceptance results |
| P2 | Dedicated evaluation |

The items below are source and document evidence. Implementation and live-server verification of every new driver item are still to begin. Task status is updated by item ID. [Development progress](opengauss-connector-progress.md)

## Build and public API

| ID | Capability | ADO baseline | JDBC or specification basis | Plan and acceptance |
| --- | --- | --- | --- | --- |
| B01 | Five TFM asset groups | The core contains two Standard and .NET Core 3.1, .NET 5/6 | OB already uses the required five target groups, see MS3 | P0 · build, pack and consume each group separately; Standard forces loading of the specified assets |
| B02 | SLNX, CPM, per-TFM package dependencies | `OpenGauss.sln`, actual versions inside the projects | OB's SLNX and conditional version groups, see MS1, MS2, MS7, MS8 | P0 · verify the five asset groups of the seven packages, the minimum dependency of the extensions on the core, and the ranged floors (`[8.0.0,)`, `[9.0.0,)`, `[10.0.0,)`) of the dependencies shipped with .NET 8/9/10 by the framework |
| B03 | NuGet ID, assembly, namespace and factory | Upstream assembly, `OpenGauss.NET` namespace and signing configuration | OB explicitly declares `PackageId`, `AssemblyName` and `RootNamespace` | P0 · verify `OpenGaussConnector.dll` inside the `OpenGaussConnector` package, the root namespace and coexistence with the legacy version |
| B04 | Connection, command, parameter, reader, transaction | `OpenGaussFactory.cs` and the public classes | JDBC API behavior can serve as semantic comparison | P0 · ADO.NET contract, factory creation, synchronous and asynchronous entry points |
| B05 | `DbBatch` and `DbDataSource` | `OpenGaussBatch.cs`, the factory entry and `Shims/Batching.cs`. No corresponding DataSource class found | OB's DataSource and Builder | P0 · verify the batch API of each asset, and the type and signature binding when a class library compiled for Standard is consumed by a modern application that selects modern assets. Modern targets implement DataSource, Builder and connection pool lifecycle. E04 tracks live-server batch behavior |
| B06 | Build-time generators and package metadata | Source generators generate connection string and type handling code | OB `eng/Verify-Packages.ps1` | P0 · check generated output, publish declarations, symbols and package dependency isolation |
| B07 | Trimming, Native AOT | There is a legacy trimming test project | Determined by host and dependency capabilities | P2 · verify trimming and AOT publishing separately, plus reflection mapping, plugins and static registration |
| B08 | `OpenGaussConnector.DependencyInjection` | ADO has no corresponding project | OB extension methods, registration and lifetime tests, see MS6, MS7 | P0 · the five asset groups support transient connections; modern targets support plain and keyed singleton data sources. Verify duplicate registration, aliases, disposal, dependency groups and standalone consumption |
| B09 | Upstream test project migration | The legacy solution only loads core tests. Some project references are broken, and several live-server fixtures fall back to a default connection string when `NPGSQL_TEST_DB` is missing | ADO project inventory and test entry points, see [M1 kickoff checklist](opengauss-connector-m1.md) | P0 · SLNX includes each test project, references are fixed and GIS cases are enabled. Offline commands execute when no connection string is configured. Live-server configuration is provided by an explicit entry point, recording actual executions and skips. Benchmark projects keep an independent run entry point |

## Connection, authentication, session

| ID | Capability | ADO baseline | JDBC or server-side basis | Plan and acceptance |
| --- | --- | --- | --- | --- |
| C01 | TCP and startup protocol | `Internal/ProtocolVersion.cs` pinned to 3.51 | The 3.0, 3.50, 3.51 branches of `core/v3/ConnectionFactoryImpl.java` | P0 · first verify 3.51 startup, server product information and parameter status |
| C02 | openGauss SHA256 | `Internal/OpenGaussConnector.Auth.cs` routes request code 10 to SHA256 | JDBC `v6.0.6` request code 10, see OG5 | P0 · check success, failure, cancellation and encoding against the official implementation and a real server |
| C03 | MD5, mixed authentication, SM3 | MD5 has a branch. 11 acts as SASL continue in the legacy parsing, no branch found for 13 | JDBC `v6.0.6` has 5, 11, 13 respectively, see OG5 | P1 · verify the legacy MD5 configuration; complete mixed authentication and SM3 according to server policy |
| C04 | Plaintext, GSS/SSPI, PostgreSQL SCRAM | Plaintext and GSS/SSPI exist. SCRAM methods exist, current startup dispatch uses openGauss SHA256 | JDBC has plaintext and GSS/SSPI branches | P2 · check protocol, OS and server configuration separately, then declare support |
| C05 | TLS, certificate verification, mTLS | `SslStream`, the connection string has SSL mode and trust options | JDBC SSL configuration, see OG4 | P0 · Require, VerifyCA, VerifyFull, invalid CA, hostname mismatch and client certificate |
| C06 | Unix domain socket | `Internal/OpenGaussConnector.cs` has a UDS path | A JDBC UDS guide exists and the development line has oGRAC cases | P1 · verify socket path, permissions, cancellation and target assets separately on Linux |
| C07 | Connection string, credentials, application name, encoding | `OpenGaussConnectionStringBuilder.cs`, pgpass and environment variable code | JDBC `PGProperty.java`, see OG3 | P0 · basic keys, credential precedence, UTF-8 and log redaction. Other encodings are verified separately |
| C08 | Connection pool, pool keys and session reset | `ConnectorPool.cs`, `PoolTests.cs` | JDBC data sources and server session parameters | P0 · reset transactions, session state and prepared state; verify leaks, concurrency and failure handling |
| C09 | Primary/standby selection, load balancing, connection failover | `MultiHostConnectorPool.cs`, `MultipleHostsTests.cs` | JDBC `hostchooser/`, `targetServerType`, see OG3 | P1 · primary/standby switchover, multi-endpoint ordering, role re-check, pool invalidation and read-only preference |
| C10 | Dynamic topology, automatic balancing, statement routing, ATF | ADO is mainly based on a static host set and session preference | JDBC has `quickautobalance/`, `readwritesplitting/` and ATF configuration | P2 · check topology, server components, transaction consistency and error semantics separately |
| C11 | oGRAC | The snapshot has no evidence of dedicated support | JDBC `v7.0.0` has `jdbc:oGRAC:` and a protocol implementation; the development line adds dedicated tests, see OG9 | P2 · verify multi-read multi-write and multi-instance session behavior independently |
| C12 | Fully encrypted and TLCP | The snapshot shows no corresponding implementation | `v5.0.3` already has JDBC `enable_ce` and a JNI entry point. TLCP properties are in `v6.0.0` | P2 · evaluate native dependencies, certificate interfaces, platform, deployment and distribution separately |
| C13 | Server version identification and capability selection | Reads the startup parameter `server_version`; catalog loading includes `SELECT version()`. Some capability checks inherit PostgreSQL version thresholds | JDBC `ConnectionFactoryImpl.queryGaussdbVersion` already uses `SELECT version()` to identify the product | P0 · validation of `Server Version=Auto` and of an explicit version; identify mode and catalog capabilities per physical connection. Test unknown versions, mixed-version nodes, and reconnect after upgrade |

## Execution, transactions, streaming and replication

| ID | Capability | ADO baseline | JDBC or server-side basis | Plan and acceptance |
| --- | --- | --- | --- | --- |
| E01 | Parameters, preparation, queries, multiple results | `OpenGaussCommand.cs`, `SqlQueryParser.cs`, command and preparation tests | `jdbc/PgPreparedStatement.java`, `core/v3/QueryExecutorImpl.java` | P0 · named and positional parameters, NULL, prepared statement count, multiple statements and multiple results |
| E02 | Transactions, isolation levels, savepoints | `OpenGaussTransaction.cs`, `TransactionTests.cs` | JDBC connection transaction and savepoint logic | P0 · commit, rollback, transaction state after an exception and savepoint recovery |
| E03 | Asynchronous, timeout, cancellation | Connections, commands and readers have asynchronous paths, legacy tests include cancellation | JDBC connection, query and cancellation timeout configuration | P0 · compare before, during and after cancellation; verify message boundaries and whether the connection can be reused |
| E04 | Batch execution and generated keys | `OpenGaussBatch.cs`, `BatchTests.cs` | JDBC batch execution and generated key cases | P1 · verify error position, rollback and row counts. Verify `RETURNING` or the available key return semantics per mode |
| E05 | Batch INSERT rewriting | The upstream has a batch execution API | JDBC `reWriteBatchedInserts`, see OG3 | P1 · evaluate the benefit separately, covering statement shapes, generated keys and mid-way failure |
| E06 | COPY text, CSV, binary | `OpenGaussBinaryImporter.cs`, `OpenGaussBinaryExporter.cs`, `OpenGaussRawCopyStream.cs` | JDBC `copy/CopyManager.java`, see OG7 | P1 · verify round trip, cancellation, termination and rollback separately per direction and format |
| E07 | Sequential reading, cursor batching, streaming parameters | There are `SequentialAccess`, buffering and read/write stream paths | JDBC `defaultRowFetchSize`, streaming parameter options | P1 · large rows, large values and mid-way cancellation. Sequential reading of a single column and server-side cursor batching are measured separately |
| E08 | Functions, procedures, OUT/INOUT, refcursor | `CommandType.StoredProcedure` of `OpenGaussCommand.cs` generates `SELECT * FROM` | JDBC callable, procedure and cursor cases, see OG12 | P1 · keep upstream function call semantics, verify explicit `CALL`, parameter direction and returned cursor separately for A, B and PG |
| E09 | DataAdapter, CommandBuilder | `OpenGaussDataAdapter.cs`, `OpenGaussCommandBuilder.cs` | ADO.NET API conventions | P1 · `Fill`, update command generation, primary keys and identifier quoting |
| E10 | Notifications, Notice, SQLSTATE, diagnostics | Notification, Notice, exception and EventSource code and tests exist | JDBC notice and exception mapping | P0 · error and SQLSTATE semantics. P1 · notification and diagnostic events |
| E11 | Physical and logical replication | `Replication/` contains physical, `PgOutput`, `TestDecoding` code | JDBC `core/v3/replication/`, see OG8 | P2 · confirm server replication privileges, slots, output plugins, LSN and recovery, declare support item by item |
| E12 | Large objects and LOB streaming | A Large Object API exists. The legacy ADO README records that the server reports it as unsupported | JDBC has LargeObjectManager and Blob/Clob paths | P2 · distinguish the PostgreSQL Large Object API, openGauss LOB types and streaming reads and writes |
| E13 | `System.Transactions`, distributed transactions | A resource manager and legacy tests exist | JDBC XA is a separate path | P2 · verify local enlistment, prepared transactions, distributed hosts and recovery separately |
| E14 | Multiplexing | `MultiplexingConnectorPool.cs` and legacy tests | JDBC multi-host and routing have semantics independent of this mechanism | P2 · after the basic pool is stable, verify ordering, transaction binding, cancellation and failure recovery |

## Types, metadata and extensions

| ID | Capability | ADO baseline | JDBC or server-side basis | Plan and acceptance |
| --- | --- | --- | --- | --- |
| T01 | Basic numeric, text, binary, Boolean, NULL | Built-in type handlers and type unit tests | JDBC `jdbc/TypeInfoCache.java` | P0 · verify parameters, results, precision and overflow per database mode and type |
| T02 | Date, timestamp, time zone, interval | Legacy code contains current and legacy mappings, plus a NodaTime plugin | JDBC time types and cases | P0 · basic round trip, time zone, boundaries. P1 · interval and legacy time semantics |
| T03 | UUID, JSON, JSONB | Legacy type handlers and JSON tests | JDBC type cache, actual OID and encoding come from the target server | P0 · UUID and basic JSON. P1 · JSONB binary and complex serialization behavior |
| T04 | Arrays, enums, composites, domain, range/multirange | General mapping code exists. The legacy README has recorded DOMAIN failures | JDBC type cache and server catalogs | P1 · arrays and common types. P2 · confirm syntax, catalogs, reads and writes and null values of complex types item by item |
| T05 | Types and semantics specific to compatibility mode | The ADO README provides A, B, PG database creation examples | `DBCOMPATIBILITY`, see OG6. The development line has separate RAW/LOB and other tests for oGRAC | P1 · determine type names, OIDs, precision, parameter inference and fallback strategy after measuring modes and versions |
| T06 | Catalogs and `GetSchema` | `OpenGaussSchema.cs`, `PostgresDatabaseInfo.cs`, the legacy README records some catalog test failures | JDBC `jdbc/PgDatabaseMetaData.java` | P0 · basic tables, columns, indexes, keys and constraints. P1 · sequences, identity, generated columns and extensions |
| T07 | Reader column description and SchemaTable | `ReaderOldSchemaTests.cs`, `ReaderNewSchemaTests.cs` | JDBC ResultSetMetaData | P0 · aliases, base table information, precision, scale, nullability and catalog permissions |
| T09 | Vectors and later new server types | The legacy ADO shows no dedicated vector mapping | The 7.0.0 enterprise edition release notes cover vector storage and indexes, see OG2 | P2 · first check the distribution form, extensions, type OIDs and text/binary protocol, then decide the CLR API |

## Official ADO.NET extension packages

All five independent upstream extensions are included in the port. For each package, the compile, pack and consume results of the five asset groups are recorded separately. Live-server capabilities are recorded per measured topology, server extension and data type. [Packages and registration entry points](opengauss-connector-design.md#official-adonet-extension-packages)

| ID | Upstream project | Ported package | First-release acceptance |
| --- | --- | --- | --- |
| X01 | `OpenGauss.GeoJSON.NET` | `OpenGaussConnector.GeoJSON` | P0 · `UseGeoJson`, EWKB round trip, geometry/geography, SRID, bounding box, legacy CRS options and a WGS 84 sample conforming to RFC 7946. See EXT1, EXT2 |
| X02 | `OpenGauss.Json.NET` | `OpenGaussConnector.Json.NET` | P0 · `UseJsonNet`, json/jsonb, POCO, NULL, text and binary, custom serialization settings and built-in mapping precedence. See EXT3 |
| X03 | `OpenGauss.NetTopologySuite` | `OpenGaussConnector.NetTopologySuite` | P0 · `UseNetTopologySuite`, geometry/geography, EWKB, SRID, Z/M and precision model, process isolation from GeoJSON. See EXT2 |
| X04 | `OpenGauss.NodaTime.NET` | `OpenGaussConnector.NodaTime` | P0 · `UseNodaTime`, date and time, time zone, infinite values, Duration/Period and legacy behavior. Ranges and multiranges are verified according to their presence on the server. See EXT4 |
| X05 | `OpenGauss.OpenTelemetry.NET` | `OpenGaussConnector.OpenTelemetry` | P0 · `AddOpenGauss`, ActivitySource subscription, success and error spans, tag redaction, behavior when listening is closed, and coexistence with legacy assemblies. See EXT5, EXT6 |

## JDBC release version differences

"Seen" means the source or setting was checked in the specified tag. The first-seen scope is limited to the tags compared in this table. JDBC tags are used to confirm changes in client behavior. Specific server capabilities are verified separately for distribution form, mode, configuration and measured results. Difference commits and comparison commands are in [references](references.md).

| Corresponding feature | 5.0.3 / 6.0.0 / 6.0.6 evidence | 7.0.0 tag and local development line | Server conditions and .NET handling |
| --- | --- | --- | --- |
| C01, C02, C03 · Startup and password authentication | `v5.0.3` already has SHA256, MD5-SHA256 and SM3 branches. `v6.0.6` supports 3.0, 3.50 and 3.51 startup messages | `v7.0.0` keeps these paths | Chosen by handshake message and authentication configuration; the product version is used for compatibility decisions after connecting |
| C09, C10 · Static primary/standby and quick balancing | `v5.0.3` already has `targetServerType`, `loadBalanceHosts` and quick balancing related configuration. `v6.0.0` already has statement routing switches | `v7.0.0` continues | Collect actual endpoint roles from the session or topology, verify per endpoint |
| C12 · Fully encrypted | `v5.0.3` already has `enable_ce` and `gauss_cl_jni` | `v7.0.0` keeps them | Requires the target server, client native components and key deployment |
| C05, C12 · TLCP | `v6.0.0` already has TLCP and encrypted certificate and key parameters, `v6.0.6` keeps them | `v7.0.0` keeps them, plus the SSL path of oGRAC | Verify by product, certificate and connection endpoint, independent of ordinary TLS |
| E05, E07 · Batch rewriting and Fetch Size | `v5.0.3` already has `reWriteBatchedInserts` and `defaultRowFetchSize` | `v7.0.0` keeps them, and additionally adds a Reader buffer cap for simple queries | Batch rewriting and cursors are verified separately. Client resource limits can cover all server versions |
| C09, E01 · Reconnect after disconnect and prepared state | `v6.0.6` already has `autoReconnect` and includes the change that cleans up prepared state after disconnect (`77acc2b`, `dfd3ca7`) | `v7.0.0` keeps the reconnect parameters and additionally adds the ATF flow | .NET invalidates the old prepared cache when it rebuilds a physical connection. Returns an error for a write or commit with unknown results |
| T05 · `bit_output` and legacy compatibility | `v6.0.6` contains compatibility fixes for 5.0 and earlier (`e75486e`) | `v7.0.0` continues that behavior and tightens parameter checks | .NET chooses the path according to real configuration support, covering missing legacy objects, insufficient permissions and actual return values |
| T06, T07 · Generated column metadata | `v6.0.6` adds `IS_GENERATEDCOLUMN` (`22a6b2e`), the SQL selects fields by compatibility version | `v7.0.0` keeps it | .NET checks generated column fields and expressions in the catalogs. Verify values and missing semantics per version and mode |
| C11 · oGRAC driver entry | The compared 6.0.6 tag has no such entry or OR protocol classes | `v7.0.0` adds `jdbc:oGRAC:`, `ORConnection`, `ORQueryExecutorImpl`, SSL and type/metadata implementations. The development line additionally adds 36 dedicated test files and modifies `ORResultSet` | The target is the separate oGRAC product and the corresponding protocol. Ordinary openGauss 7.0.0 connections still use the ordinary handshake |
| C10, E02 · ATF session and transaction recovery | `v6.0.6` already has basic reconnect configuration | `v7.0.0` adds ATF switches, service addresses, caching and result checks (`8c8ea6b` and others) | Dedicated deployment verifies transaction consistency and failure boundaries. Reconnection of the base driver keeps the original request failure semantics |
| T09 · Multi-vector concurrent queries | `v6.0.6` has no `util/ParallelSearch.java` | `v7.0.0` adds a Java client query helper class (`d2950de`) | The 7.0.0 enterprise edition release notes record native vector storage and indexes. Query the target environment's extensions, operators, types and indexes separately, and decide the .NET API after verification |
| E06, E07 · COPY and streaming resource boundaries | Legacy tags already have COPY and streaming parameters | `v7.0.0` adds `maxCopyDataReceiveBytes`, `maxBufferedStreamParameterChars`, and adds tests | Client resource limits apply to each supported version. Actual COPY formats and cancellation semantics of each version still require live-server verification |
| C05, E11 · TLS and replication hardening | Existing TLS validation and replication paths | `v7.0.0` hardens the certificate verification path and replication slot identifier validation, and adds resource boundary tests | Include the applicable security behavior in the shared .NET implementation, each environment uses the same validation rules |

## Server acceptance status

| Server | A | B | PG | Other combinations |
| --- | --- | --- | --- | --- |
| openGauss 6.0.6 LTS | Pending verification | Pending verification | Pending verification | The 6.0.0 database creation documentation separately lists mode C. Distribution form, primary/standby and extensions are recorded separately |
| openGauss 7.0.0 LTS | Pending verification | Pending verification | Pending verification | The enterprise edition release notes involve database D, confirm according to the available environments |
| Older versions | Candidate | Candidate | Candidate | JDBC 5.0.3 serves as version difference evidence. Server patch releases are selected according to actual deployment needs |
| oGRAC 7.0.0 LTS | Dedicated evaluation pending | Dedicated evaluation pending | Dedicated evaluation pending | Actual supported modes are subject to the oGRAC environment |

The server 7.0.0 enterprise edition release notes explicitly record the change of the default compatibility option of database D, and an upgrade can keep the original configuration. Version identification and capability probing therefore need to record the product, version, mode and actual session configuration at the same time. The installed version and upgrade history of the user environment are to be confirmed. Each feature can be registered per combination as source-confirmed, offline pass, live-server pass, environment missing, server not applicable or driver implementation pending. When judging the server not applicable, record the actual response, release version or upstream comparison. [Development progress](opengauss-connector-progress.md)
