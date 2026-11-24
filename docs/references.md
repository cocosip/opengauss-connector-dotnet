# 参考资料

源码核对日期为 2026-11-24。以下 checkout 路径以本仓库根目录为基准。文档中的源码文件路径以对应参考仓库根目录为基准。

## 本地源码

| 编号 | 仓库及相对路径 | 核对基线 | 主要入口 |
| --- | --- | --- | --- |
| ADO | `../../dotnet-source/openGauss-connector-adonet` | `ba5e8ba629b0d660d095da6d8af94f487a00730c`，2023-11-07 | `README.md`、`LICENSE`、`Directory.Build.props`、`src/OpenGauss.NET/`、`src/OpenGauss.SourceGenerators/`、`test/OpenGauss.Tests/` |
| JDBC | `../../java-source/openGauss-connector-jdbc` | 本地 `master` 为 `f2b803a661d4e5e42464ca57377e10c790922c78`，2026-11-09。发布标签对应提交为 `v5.0.3`（`9283fdf`）、`v6.0.0`（`9deecee`）、`v6.0.6`（`f7ee5a4`）、`v7.0.0`（`6e51e28`） | `pgjdbc/src/main/java/org/postgresql/`、`pgjdbc/src/test/java/org/postgresql/`、`pgjdbc/src/test/java/org/ograc/`、`README_cn.md`、`LICENSE`、`Third_Party_Open_Source_Software_Notice` |
| OB | `../obconnector-dotnet` | `903e103298ee59ef0ee9c3b08a3337952d5e2c60`，2026-11-23 | `OceanBaseConnector.slnx`、`Directory.Build.props`、`Directory.Packages.props`、`src/OceanBaseConnector/`、`src/OceanBaseConnector.DependencyInjection/`、`tests/OceanBaseConnector.ConsumerTests.NetStandard/`、`eng/Verify-Packages.ps1` |

### 官方 ADO.NET 项目清单

| 上游项目或目录 | 核对入口 |
| --- | --- |
| `src/OpenGauss.NET/` | 核心库。版本能力、池和调用语义参考 `Internal/OpenGaussDatabaseInfo.cs`、`OpenGaussDatabaseInfoCacheKey.cs`、`OpenGaussCommand.cs`，追踪参考 `OpenGaussActivitySource.cs` |
| `src/OpenGauss.GeoJSON.NET/` | 项目文件、`OpenGaussGeoJSONExtensions.cs`、`GeoJSONOptions.cs`、`Internal/GeoJSONHandler.cs`，用例 `test/OpenGauss.PluginTests/GeoJSONTests.cs` |
| `src/OpenGauss.Json.NET/` | 项目文件、`OpenGaussJsonNetExtensions.cs`，用例 `test/OpenGauss.PluginTests/JsonNetTests.cs` |
| `src/OpenGauss.NetTopologySuite/` | 项目文件、`OpenGaussNetTopologySuiteExtensions.cs`，用例 `test/OpenGauss.PluginTests/NetTopologySuiteTests.cs` |
| `src/OpenGauss.NodaTime.NET/` | 项目文件、`OpenGaussNodaTimeExtensions.cs`，用例 `test/OpenGauss.NodaTime.Tests/` |
| `src/OpenGauss.OpenTelemetry.NET/` | 项目文件、`TracerProviderBuilderExtensions.cs` 和 `README.md` |
| `src/OpenGauss.SourceGenerators/` | 内部生成器，项目文件声明 `IsPackable=false` |
| `test/` | 核心、插件、NodaTime、ADO.NET 规范、裁剪五个测试项目，以及基准项目。旧 `OpenGauss.sln` 只加载核心测试。插件、NodaTime、规范、裁剪和基准项目含旧项目引用。GIS 用例受 `TestGeoJSONTests`、`NetTopologySuiteTest` 条件编译控制。 |

ADO.NET 的 `Directory.Build.props` 中 `VersionPrefix` 为 `6.0.3`，表示上游构建版本。JDBC 本地 `master` 的 `pgjdbc/pom.xml` 标为 8.0.0-RC1，按开发线线索使用。发布内容以对应标签为证据。6.0.6 与 7.0.0 的分支历史有各自的提交，使用标签树之间的差异核对代码保留情况。

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

| JDBC 对照 | 代表提交和文件 |
| --- | --- |
| `v6.0.0` 到 `v6.0.6` | `77acc2b` 重连配置，`dfd3ca7` 重连后预准备状态，`22a6b2e` 生成列，`e75486e` 旧服务端 `bit_output` 兼容 |
| `v6.0.6` 与 `v7.0.0` 的发布差异 | `e4ef598` oGRAC，`8c8ea6b` ATF 恢复流程，`d2950de` 多向量查询，`08a89fc` 流式缓冲限制，`5caf8f5` COPY 接收限制 |
| `v7.0.0` 到本地开发线 | `pgjdbc/src/test/java/org/ograc/` 新增用例，`jdbc/ORResultSet.java` 修改 |

## openGauss 官方资料

| 编号 | 资料 | 用途 |
| --- | --- | --- |
| OG1 | [openGauss 社区下载](https://opengauss.org/zh/download/) | 6.0.6 LTS、7.0.0 LTS 与社区分发 |
| OG2 | [openGauss 7.0.0 LTS 发行说明](https://docs.opengauss.org/zh/docs/7.0.0/release_notes/release_notes.html) | 企业版新增特性、兼容性变更和产品能力 |
| OG3 | [JDBC 连接参数，6.0.0 文档](https://docs.opengauss.org/en/docs/6.0.0/docs/DeveloperGuide/connecting-to-a-database-JDBC.html) | 主备选择、连接选项、fetch size、批量改写、全密态入口 |
| OG4 | [JDBC SSL，6.0.0 文档](https://docs.opengauss.org/en/docs/6.0.0/docs/DeveloperGuide/connecting-to-a-database-using-ssl-jdbc.html) | SSL 模式、证书配置 |
| OG5 | [客户端访问认证](https://docs.opengauss.org/en/docs/6.0.0/docs/DatabaseAdministrationGuide/client-access-authentication.html)、[安全和认证参数](https://docs.opengauss.org/en/docs/6.0.0/docs/DatabaseReference/security-and-authentication_postgresql-conf.html) | `pg_hba.conf`、`password_encryption_type`、SHA256 和 SM3 |
| OG6 | [CREATE DATABASE，6.0.0 文档](https://docs.opengauss.org/en/docs/6.0.0/docs/SQLReference/create-database.html) | `DBCOMPATIBILITY` 和建库参数 |
| OG7 | [COPY，6.0.0 文档](https://docs.opengauss.org/en/docs/6.0.0/docs/SQLReference/copy.html) | STDIN/STDOUT 和文本、CSV、二进制选项 |
| OG8 | [逻辑解码，6.0.0 文档](https://docs.opengauss.org/en/docs/6.0.0/docs/DatabaseOMGuide/logical-decoding.html) | 服务端逻辑解码入口 |
| OG9 | [oGRAC 7.0.0 LTS 发行说明](https://docs.opengauss.org/zh/docs/7.0.0/ograc/release_notes/release_notes.html) | 多读多写、多实例部署及 JDBC 入口 |
| OG10 | [官方 ADO.NET 源码](https://gitcode.com/opengauss/openGauss-connector-adonet)、[官方 JDBC 源码](https://gitcode.com/opengauss/openGauss-connector-jdbc) | 上游代码与许可证 |
| OG11 | [openGauss 6.0.0 PostGIS 支持与限制](https://docs.opengauss.org/en/docs/6.0.0/docs/ExtensionReference/postgis-support-and-constraints.html)、[openGauss 7.0.0 PostGIS 扩展](https://docs.opengauss.org/zh/docs/7.0.0/extension_reference/extension_reference/third_party/postgis.html) | 6.0.0 文档列出 `geometry` 和 `geography`。7.0.0 文档列出 PostGIS 2.4.2 及安装步骤。 |
| OG12 | [openGauss 6.0.0 CALL](https://docs.opengauss.org/en/docs/6.0.0/docs/SQLReference/call.html) | 函数、过程、输出参数和权限语义 |
| OG13 | [平台和客户端兼容性，6.0.0 文档](https://docs.opengauss.org/en/docs/6.0.0/docs/DatabaseReference/platform-and-client-compatibility.html) | `sql_compatibility` 是可读取的内部参数。具体发行版本和模式以实测为准。 |

OG3–OG8 来自 6.0.0 文档。测试报告应记录实测补丁版、发行形态、兼容模式、扩展和节点拓扑。7.0.0 在 OG2 列出的服务端能力仍需逐项验证客户端接口、类型和协议。

## .NET 与 NuGet 官方资料

| 编号 | 资料 | 用途 |
| --- | --- | --- |
| MS1 | [Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) | `Directory.Packages.props`、按 TFM 条件声明版本、传递锁定及 pack 影响 |
| MS2 | [dotnet sln](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln) | `.slnx` 管理及 CLI 使用 |
| MS3 | [.NET Standard](https://learn.microsoft.com/en-us/dotnet/standard/net-standard) | Standard 目标框架和消费宿主兼容性 |
| MS4 | [.NET 支持策略](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) | 运行时维护窗口 |
| MS5 | [NuGet V3 Package Base Address](https://learn.microsoft.com/en-us/nuget/api/package-base-address-resource) | 包 ID 的公开索引查询 |
| MS6 | [Microsoft DI 指南](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection-guidelines) | 容器所有权、释放和生命周期 |
| MS7 | [DI Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection.Abstractions/8.0.0)、[DI 容器](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/9.0.0)、[Logging Abstractions](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Abstractions/10.0.0) | 已通过 NuGet V3 `.nuspec` 核对三包的 `8.0.0`、`9.0.0`、`10.0.0` 共九组元数据。DI 两包的 8.0.0 有两个 Standard 依赖组，Logging Abstractions 8.0.0 提供 Standard 2.0 组。 |
| MS8 | [.NET Package Validation](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview)、[NuGet 包兼容规则](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/nuget-package-compatibility-rules)、[DbDataSource](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbdatasource) | 跨 TFM 公共 API 兼容性与现代 data source 合约 |
| MS9 | [DbProviderFactories.RegisterFactory](https://learn.microsoft.com/en-us/dotnet/api/system.data.common.dbproviderfactories.registerfactory)、[.NET Framework 工厂配置](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/obtaining-a-dbproviderfactory) | 工厂注册 API 的可用目标与 .NET Framework 配置方式 |
| MS10 | [VSTest 按条件运行测试](https://learn.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests) | NUnit 类别、xUnit Trait 和 `dotnet test --filter` |

2026-11-24，NuGet.org 上精确 ID `OpenGaussConnector` 的包详情页和 V3 包版本索引均返回 HTTP 404。同一版本索引服务对 `Newtonsoft.Json` 返回 HTTP 200。设计采用 `OpenGaussConnector` 作为核心包 ID。 [包详情页](https://www.nuget.org/packages/OpenGaussConnector) · [V3 包版本索引](https://api.nuget.org/v3-flatcontainer/opengaussconnector/index.json)

同日，`OpenGaussConnector.DependencyInjection` 的 V3 包版本索引返回 HTTP 404。 [DI 包版本索引](https://api.nuget.org/v3-flatcontainer/opengaussconnector.dependencyinjection/index.json)

同日五个扩展包 ID 的精确 V3 查询均返回 HTTP 404。 [GeoJSON](https://api.nuget.org/v3-flatcontainer/opengaussconnector.geojson/index.json) · [Json.NET](https://api.nuget.org/v3-flatcontainer/opengaussconnector.json.net/index.json) · [NetTopologySuite](https://api.nuget.org/v3-flatcontainer/opengaussconnector.nettopologysuite/index.json) · [NodaTime](https://api.nuget.org/v3-flatcontainer/opengaussconnector.nodatime/index.json) · [OpenTelemetry](https://api.nuget.org/v3-flatcontainer/opengaussconnector.opentelemetry/index.json)

## 扩展包规范与上游资料

| 编号 | 资料 | 用途 |
| --- | --- | --- |
| EXT1 | [RFC 7946 GeoJSON](https://www.rfc-editor.org/rfc/rfc7946.html) | WGS 84 经纬度、包围盒和旧 CRS 语义 |
| EXT2 | [PostGIS ST_AsEWKB](https://postgis.net/docs/ST_AsEWKB.html)、[openGauss PostGIS 扩展](https://docs.opengauss.org/zh/docs/7.0.0/extension_reference/extension_reference/third_party/postgis.html) | EWKB 的 SRID 与几何维度，及目标服务安装条件 |
| EXT3 | [Json.NET Serialization Settings](https://www.newtonsoft.com/json/help/html/SerializationSettings.htm) | 自定义设置、`TypeNameHandling`、`ISerializationBinder` |
| EXT4 | [NodaTime Core Types](https://nodatime.org/3.2.x/userguide/core-types) | `Instant`、`Duration` 与 `Period` 的映射边界 |
| EXT5 | [OpenTelemetry .NET Instrumentation](https://opentelemetry.io/docs/languages/dotnet/instrumentation/) | `ActivitySource` 的发射及订阅 |
| EXT6 | [OpenTelemetry 数据库 span 语义约定](https://opentelemetry.io/docs/specs/semconv/database/database-spans/) | 规范化 span 标签、错误和查询文本处理 |
