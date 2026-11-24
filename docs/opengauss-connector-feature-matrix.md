# openGauss ADO.NET 功能矩阵

2026-11-24 · 源码调研基线

ADO 指官方 ADO.NET 固定快照。功能表用于跟踪 .NET 移植。JDBC 版本差异表给出 `v5.0.3`、`v6.0.0`、`v6.0.6`、`v7.0.0` 与本地开发线的证据。表中提到的 ADO 核心文件位于 `src/OpenGauss.NET/`，JDBC 相应文件位于 `pgjdbc/src/main/java/org/postgresql/`。官方资料编号见 [参考资料](references.md)。

| 优先级 | 含义 |
| --- | --- |
| P0 | 首发必须交付并通过验收 |
| P1 | 优先移植或增强，按联机验收结果纳入首发或后续版本 |
| P2 | 专项评估 |

下列条目是源码与文档证据。当前新驱动各项实现与联机验证均待开始。任务状态按条目 ID 更新。 [开发进度](opengauss-connector-progress.md)

## 构建与公共接口

| ID | 能力 | ADO 基线 | JDBC 或规范依据 | 计划与验收 |
| --- | --- | --- | --- | --- |
| B01 | 五组 TFM 资产 | 核心含两个 Standard 及 .NET Core 3.1、.NET 5/6 | OB 已使用所需五组目标，见 MS3 | P0 · 对每组分别 build、pack、消费，Standard 强制加载指定资产 |
| B02 | SLNX、CPM、按 TFM 的包依赖 | `OpenGauss.sln`，实际版本在项目内 | OB 的 SLNX 与条件版本分组，见 MS1、MS2、MS7、MS8 | P0 · 验证七个包的五组资产、扩展对核心的依赖下限，以及 .NET 8/9/10 随框架发行依赖的下限 `8.0.0`、`9.0.0`、`10.0.0` |
| B03 | NuGet ID、程序集、命名空间和工厂 | 上游程序集、`OpenGauss.NET` 命名空间及签名配置 | OB 显式声明 `PackageId`、`AssemblyName` 和 `RootNamespace` | P0 · 验证 `OpenGaussConnector` 包内的 `OpenGaussConnector.dll`、根命名空间及旧版共存 |
| B04 | 连接、命令、参数、读取器、事务 | `OpenGaussFactory.cs` 和各公共类 | JDBC API 行为可作语义对照 | P0 · ADO.NET 合约、工厂创建、同步与异步入口 |
| B05 | `DbBatch` 与 `DbDataSource` | `OpenGaussBatch.cs`、工厂入口及 `Shims/Batching.cs`。未见对应 DataSource 类 | OB 的 DataSource 与 Builder | P0 · 验证各资产的批量 API，以及 Standard 编译的类库在现代应用选用现代资产时的类型和签名绑定。现代目标实现 DataSource、Builder 和连接池生命周期。E04 跟踪批量联机行为 |
| B06 | 构建期生成器与包元数据 | 源生成器生成连接串与类型处理代码 | OB `eng/Verify-Packages.ps1` | P0 · 检查生成结果、发布声明、符号及包依赖隔离 |
| B07 | Trimming、Native AOT | 有旧版 trimming 测试项目 | 由宿主与依赖能力决定 | P2 · 分别验证裁剪和 AOT 发布，以及反射映射、插件和静态注册 |
| B08 | `OpenGaussConnector.DependencyInjection` | ADO 无对应工程 | OB 扩展方法、注册及生命周期测试，见 MS6、MS7 | P0 · 五组资产支持瞬态连接，现代目标支持普通和键控单例数据源。验证重复注册、别名、释放、依赖组和独立消费 |
| B09 | 上游测试项目迁移 | 旧解决方案只加载核心测试。部分项目引用失效，多个联机夹具在缺少 `NPGSQL_TEST_DB` 时回退到默认连接串 | ADO 项目清单与测试入口，见 [M1 开工清单](opengauss-connector-m1.md) | P0 · SLNX 纳入各测试项目，修复引用并开启 GIS 用例。离线命令在无连接串配置时执行。联机配置由显式入口提供，记录实际执行及跳过数。基准项目保留独立运行入口 |

## 连接、认证、会话

| ID | 能力 | ADO 基线 | JDBC 或服务端依据 | 计划与验收 |
| --- | --- | --- | --- | --- |
| C01 | TCP 与启动协议 | `Internal/ProtocolVersion.cs` 固定 3.51 | `core/v3/ConnectionFactoryImpl.java` 的 3.0、3.50、3.51 分支 | P0 · 首先验证 3.51 启动、服务端产品信息和参数状态 |
| C02 | openGauss SHA256 | `Internal/OpenGaussConnector.Auth.cs` 请求号 10 走 SHA256 | JDBC `v6.0.6` 请求号 10，见 OG5 | P0 · 用官方实现与真实服务端核对成功、失败、取消和编码 |
| C03 | MD5、混合认证、SM3 | MD5 有分支。11 在旧解析中作 SASL continue，13 未见分支 | JDBC `v6.0.6` 分别有 5、11、13，见 OG5 | P1 · 验证 MD5 旧配置，按服务端策略补齐混合认证和 SM3 |
| C04 | 明文、GSS/SSPI、PostgreSQL SCRAM | 有明文和 GSS/SSPI。存在 SCRAM 方法，当前启动分派走 openGauss SHA256 | JDBC 有明文、GSS/SSPI 分支 | P2 · 分别核对协议、OS 和服务端配置，再声明支持 |
| C05 | TLS、证书验证、mTLS | `SslStream`，连接串有 SSL 模式与信任选项 | JDBC SSL 配置，见 OG4 | P0 · Require、VerifyCA、VerifyFull、无效 CA、主机名不符和客户端证书 |
| C06 | Unix domain socket | `Internal/OpenGaussConnector.cs` 有 UDS 路径 | 有 JDBC UDS 指南和开发线 oGRAC 用例 | P1 · Linux 上分别验证套接字路径、权限、取消和目标资产 |
| C07 | 连接串、凭据、应用名、编码 | `OpenGaussConnectionStringBuilder.cs`、pgpass 与环境变量代码 | JDBC `PGProperty.java`，见 OG3 | P0 · 基础键、凭据优先级、UTF-8 与日志脱敏。其他编码单独验证 |
| C08 | 连接池、池键和会话复位 | `ConnectorPool.cs`、`PoolTests.cs` | JDBC 数据源与服务端会话参数 | P0 · 复位事务、会话状态和预准备状态，验证泄漏、并发和故障处置 |
| C09 | 主备选择、负载均衡、连接故障转移 | `MultiHostConnectorPool.cs`、`MultipleHostsTests.cs` | JDBC `hostchooser/`、`targetServerType`，见 OG3 | P1 · 主备切换、多端点顺序、角色复检、池失效和只读偏好 |
| C10 | 动态拓扑、自动均衡、语句分流、ATF | ADO 主要基于静态主机集合和会话偏好 | JDBC 有 `quickautobalance/`、`readwritesplitting/` 及 ATF 配置 | P2 · 分别核对拓扑、服务端组件、事务一致性与错误语义 |
| C11 | oGRAC | 快照未形成专项支持证据 | JDBC `v7.0.0` 有 `jdbc:oGRAC:` 与协议实现，开发线补充专项测试，见 OG9 | P2 · 独立验证多读多写和多实例会话行为 |
| C12 | 全密态与 TLCP | 快照未见相应实现 | `v5.0.3` 已有 JDBC `enable_ce` 及 JNI 入口。TLCP 属性见 `v6.0.0` | P2 · 分别评估原生依赖、证书接口、平台、部署和分发 |
| C13 | 服务端版本识别与能力选择 | 读取启动参数 `server_version`，目录加载含 `SELECT version()`。部分能力判断继承 PostgreSQL 版本阈值 | JDBC `ConnectionFactoryImpl.queryGaussdbVersion` 已用 `SELECT version()` 识别产品 | P0 · `Server Version=Auto` 与显式版本校验，按物理连接识别模式和目录能力。测试未知版本、混合版本节点、升级后重连 |

## 执行、事务、流式与复制

| ID | 能力 | ADO 基线 | JDBC 或服务端依据 | 计划与验收 |
| --- | --- | --- | --- | --- |
| E01 | 参数、预准备、查询、多结果 | `OpenGaussCommand.cs`、`SqlQueryParser.cs`、命令与准备测试 | `jdbc/PgPreparedStatement.java`、`core/v3/QueryExecutorImpl.java` | P0 · 命名与位置参数、NULL、预准备计数、多语句和多结果 |
| E02 | 事务、隔离级别、保存点 | `OpenGaussTransaction.cs`、`TransactionTests.cs` | JDBC connection 事务与保存点逻辑 | P0 · 提交、回滚、异常后事务状态和保存点恢复 |
| E03 | 异步、超时、取消 | 连接、命令和读取器有异步路径，旧测试含取消 | JDBC 的连接、查询和取消超时配置 | P0 · 取消前、中、后对照，验证消息边界与连接能否复用 |
| E04 | 批量执行与生成键 | `OpenGaussBatch.cs`、`BatchTests.cs` | JDBC 批量执行与生成键用例 | P1 · 验证错误位置、回滚和行数。按模式验证 `RETURNING` 或可用的键返回语义 |
| E05 | 批量 INSERT 改写 | 上游有批量执行 API | JDBC `reWriteBatchedInserts`，见 OG3 | P1 · 单独评估收益，覆盖语句形状、生成键和中途失败 |
| E06 | COPY 文本、CSV、二进制 | `OpenGaussBinaryImporter.cs`、`OpenGaussBinaryExporter.cs`、`OpenGaussRawCopyStream.cs` | JDBC `copy/CopyManager.java`，见 OG7 | P1 · 按方向和格式分别验证往返、取消、终止与回滚 |
| E07 | 顺序读取、游标分批、流式参数 | 有 `SequentialAccess`、缓冲与读写流路径 | JDBC `defaultRowFetchSize`、流式参数选项 | P1 · 大行、大值和中途取消。单列顺序读与服务端游标分批分别实测 |
| E08 | 函数、过程、OUT/INOUT、refcursor | `OpenGaussCommand.cs` 的 `CommandType.StoredProcedure` 生成 `SELECT * FROM` | JDBC callable、procedure、cursor 用例，见 OG12 | P1 · 保留上游函数调用语义，按 A、B、PG 分别验证显式 `CALL`、参数方向和返回游标 |
| E09 | DataAdapter、CommandBuilder | `OpenGaussDataAdapter.cs`、`OpenGaussCommandBuilder.cs` | ADO.NET API 约定 | P1 · `Fill`、更新命令生成、主键和标识符引号 |
| E10 | 通知、Notice、SQLSTATE、诊断 | 有通知、Notice、异常与 EventSource 代码及测试 | JDBC notice 与异常映射 | P0 · 错误和 SQLSTATE 语义。P1 · 通知和诊断事件 |
| E11 | 物理及逻辑复制 | `Replication/` 含物理、`PgOutput`、`TestDecoding` 代码 | JDBC `core/v3/replication/`，见 OG8 | P2 · 确认服务器复制权限、槽、输出插件、LSN 与恢复，逐个声明支持 |
| E12 | 大对象与 LOB 流 | 有 Large Object API。旧 ADO README 记载服务端报告不支持 | JDBC 有 LargeObjectManager 与 Blob/Clob 路径 | P2 · 区分 PostgreSQL Large Object API、openGauss LOB 类型和流式读写 |
| E13 | `System.Transactions`、分布式事务 | 有资源管理器与旧测试 | JDBC XA 为另一路径 | P2 · 分开验证本地 enlistment、准备事务、分布式宿主及恢复 |
| E14 | Multiplexing | `MultiplexingConnectorPool.cs` 和旧测试 | JDBC 的多主机和分流与此机制有独立语义 | P2 · 基本池稳定后验证排序、事务绑定、取消及故障恢复 |

## 类型、元数据与扩展

| ID | 能力 | ADO 基线 | JDBC 或服务端依据 | 计划与验收 |
| --- | --- | --- | --- | --- |
| T01 | 基础数值、文本、二进制、布尔、NULL | 内置类型处理器及类型单测 | JDBC `jdbc/TypeInfoCache.java` | P0 · 按数据库模式与类型验证参数、结果、精度和溢出 |
| T02 | 日期、时间戳、时区、interval | 旧代码含当前和 legacy 映射，另有 NodaTime 插件 | JDBC 时间类型与用例 | P0 · 基础往返、时区、边界。P1 · interval 和旧时间语义 |
| T03 | UUID、JSON、JSONB | 旧类型处理器与 JSON 测试 | JDBC 类型缓存，实际 OID 与编码取自目标服务端 | P0 · UUID 与基础 JSON。P1 · JSONB 二进制和复杂序列化行为 |
| T04 | 数组、枚举、复合、domain、range/multirange | 有通用映射代码。旧 README 已记录 DOMAIN 失败 | JDBC 类型缓存和服务端目录 | P1 · 数组及常用类型。P2 · 逐项确认复杂类型的语法、目录、读写和空值 |
| T05 | 兼容模式特有类型和语义 | ADO README 提供 A、B、PG 建库例子 | `DBCOMPATIBILITY`，见 OG6。开发线 oGRAC 另有 RAW/LOB 等测试 | P1 · 实测模式与版本后确定类型名、OID、精度、参数推断与回退策略 |
| T06 | 目录与 `GetSchema` | `OpenGaussSchema.cs`、`PostgresDatabaseInfo.cs`，旧 README 记录部分目录测试失败 | JDBC `jdbc/PgDatabaseMetaData.java` | P0 · 基础表、列、索引、键和约束。P1 · 序列、identity、生成列与扩展 |
| T07 | Reader 列描述及 SchemaTable | `ReaderOldSchemaTests.cs`、`ReaderNewSchemaTests.cs` | JDBC ResultSetMetaData | P0 · 别名、基础表信息、精度、标度、可空性和目录权限 |
| T09 | 向量和后续服务端新类型 | 旧 ADO 未见向量专项映射 | 7.0.0 企业版发行说明含向量存储及索引，见 OG2 | P2 · 先查发行形态、扩展、类型 OID 和文本/二进制协议，再决定 CLR API |

## 官方 ADO.NET 扩展包

五个独立上游扩展均纳入移植。每个包单独记录五组资产的编译、打包和消费结果。联机能力按实测拓扑、服务端扩展和数据类型记录。 [包及注册入口](opengauss-connector-design.md#官方-adonet-扩展包)

| ID | 上游项目 | 移植包 | 首发验收 |
| --- | --- | --- | --- |
| X01 | `OpenGauss.GeoJSON.NET` | `OpenGaussConnector.GeoJSON` | P0 · `UseGeoJson`、EWKB 往返、geometry/geography、SRID、包围盒、旧 CRS 选项及符合 RFC 7946 的 WGS 84 样例。参见 EXT1、EXT2 |
| X02 | `OpenGauss.Json.NET` | `OpenGaussConnector.Json.NET` | P0 · `UseJsonNet`、json/jsonb、POCO、NULL、文本和二进制、自定义序列化设置及内置映射优先级。参见 EXT3 |
| X03 | `OpenGauss.NetTopologySuite` | `OpenGaussConnector.NetTopologySuite` | P0 · `UseNetTopologySuite`、geometry/geography、EWKB、SRID、Z/M 和精度模型，与 GeoJSON 同进程隔离。参见 EXT2 |
| X04 | `OpenGauss.NodaTime.NET` | `OpenGaussConnector.NodaTime` | P0 · `UseNodaTime`、日期时间、时区、无穷值、Duration/Period 和旧版行为。范围与多范围按服务端存在情况验证。参见 EXT4 |
| X05 | `OpenGauss.OpenTelemetry.NET` | `OpenGaussConnector.OpenTelemetry` | P0 · `AddOpenGauss`、ActivitySource 订阅、成功和错误 span、标签脱敏、关闭监听时的行为及旧程序集共存。参见 EXT5、EXT6 |

## JDBC 发布版本差异

「已见」表示在指定标签中核对到源码或设置。首见范围限于本表对比的标签。JDBC 标签用于确认客户端行为的变化。具体服务端能力单独核验发行形态、模式、配置与实测结果。差异提交和对比命令见 [参考资料](references.md)。

| 对应功能 | 5.0.3 / 6.0.0 / 6.0.6 证据 | 7.0.0 标签与本地开发线 | 服务端条件和 .NET 处理 |
| --- | --- | --- | --- |
| C01、C02、C03 · 启动与密码认证 | `v5.0.3` 已有 SHA256、MD5-SHA256、SM3 分支。`v6.0.6` 支持 3.0、3.50、3.51 启动报文 | `v7.0.0` 保留这些路径 | 由握手报文与认证配置选择处理，产品版本用于连接后的兼容判断 |
| C09、C10 · 静态主备和快速均衡 | `v5.0.3` 已有 `targetServerType`、`loadBalanceHosts` 和快速均衡相关配置。`v6.0.0` 已有语句分流开关 | `v7.0.0` 延续 | 从会话或拓扑采集端点实际角色，逐端点验证 |
| C12 · 全密态 | `v5.0.3` 已有 `enable_ce` 和 `gauss_cl_jni` | `v7.0.0` 保留 | 需目标服务端、客户端原生组件和密钥部署 |
| C05、C12 · TLCP | `v6.0.0` 已有 TLCP 及加密证书、密钥参数，`v6.0.6` 保留 | `v7.0.0` 保留，另有 oGRAC 的 SSL 路径 | 按产品、证书和连接端点验证，独立于普通 TLS |
| E05、E07 · 批量改写和 Fetch Size | `v5.0.3` 已有 `reWriteBatchedInserts` 和 `defaultRowFetchSize` | `v7.0.0` 保留，另增加简单查询的 Reader 缓冲上限 | 批量改写和游标各自验证。客户端资源限制可覆盖全部服务端版本 |
| C09、E01 · 断连重连和预准备状态 | 在 `v6.0.6` 已有 `autoReconnect` 且包含断线后清理预准备状态改动（`77acc2b`、`dfd3ca7`） | `v7.0.0` 保留重连参数，另加入 ATF 流程 | .NET 重建物理连接时使旧预准备缓存失效。出现未知结果的写入或提交时返回错误 |
| T05 · `bit_output` 与旧版兼容 | `v6.0.6` 含针对 5.0 及之前的兼容修复（`e75486e`） | `v7.0.0` 延续该行为并收紧参数检查 | .NET 根据真实配置支持情况选择路径，覆盖旧版对象缺失、权限不足和实际返回值 |
| T06、T07 · 生成列元数据 | `v6.0.6` 增加 `IS_GENERATEDCOLUMN`（`22a6b2e`），SQL 按兼容版本选择字段 | `v7.0.0` 保留 | .NET 检查目录中生成列字段和表达式。逐版本、模式验证值及缺失语义 |
| C11 · oGRAC 驱动入口 | 对照的 6.0.6 标签无该入口及 OR 协议类 | `v7.0.0` 增加 `jdbc:oGRAC:`、`ORConnection`、`ORQueryExecutorImpl`、SSL 和类型/元数据实现。开发线另外新增 36 个专项测试文件，并修改 `ORResultSet` | 目标为独立 oGRAC 产品和相应协议。普通 openGauss 7.0.0 连接仍使用普通握手 |
| C10、E02 · ATF 会话和事务恢复 | `v6.0.6` 已有基础重连配置 | `v7.0.0` 增加 ATF 开关、服务地址、缓存和结果检查（`8c8ea6b` 等） | 专项部署验证事务一致性与故障边界。基础驱动的重连保持原请求失败语义 |
| T09 · 多向量并发查询 | `v6.0.6` 无 `util/ParallelSearch.java` | `v7.0.0` 新增 Java 客户端查询辅助类（`d2950de`） | 7.0.0 企业版发行说明记录原生向量存储与索引。分别查询目标环境的扩展、操作符、类型及索引，验证后再决定 .NET API |
| E06、E07 · COPY 与流式资源边界 | 旧标签已有 COPY 和流式参数 | `v7.0.0` 新增 `maxCopyDataReceiveBytes`、`maxBufferedStreamParameterChars`，并补充测试 | 客户端资源限制应用于各受支持版本。各版本实际 COPY 格式与取消语义仍需联机验证 |
| C05、E11 · TLS 与复制加固 | 既有 TLS 校验和复制路径 | `v7.0.0` 加强证书验证路径和复制槽标识符校验，并补充资源边界测试 | 将适用的安全行为纳入 .NET 共享实现，各环境使用相同校验规则 |

## 服务端验收状态

| 服务端 | A | B | PG | 其他组合 |
| --- | --- | --- | --- | --- |
| openGauss 6.0.6 LTS | 待验证 | 待验证 | 待验证 | 6.0.0 建库文档另列 C 模式。发行形态、主备、扩展分别记录 |
| openGauss 7.0.0 LTS | 待验证 | 待验证 | 待验证 | 企业版发行说明涉及 D 库，按可用环境确认 |
| 较旧版本 | 候选 | 候选 | 候选 | JDBC 5.0.3 作为版本差异证据。服务端补丁版按实际部署需求选取 |
| oGRAC 7.0.0 LTS | 专项待评估 | 专项待评估 | 专项待评估 | 实际支持模式以 oGRAC 环境为准 |

服务端 7.0.0 企业版发行说明明确记录 D 库的默认兼容选项变化，升级可保留原配置。版本识别和功能探测因此需要同时记录产品、版本、模式和实际会话配置。用户环境的实装版本与升级历史待确认。每个功能可按组合登记为源码已确认、离线通过、联机通过、环境缺失、服务端不适用或驱动待实现。判断服务端不适用时记录实际响应、发行版本或上游对照。 [开发进度](opengauss-connector-progress.md)
