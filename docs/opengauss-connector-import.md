# 上游导入与重命名记录

2026-11-24 · 适用于 `docs/opengauss-connector-m1.md` 步骤 1 与步骤 2

本文件记录官方 ADO.NET 快照导入本仓库的固定基线、路径映射、机械重命名规则、与上游的显式偏离，以及每次导入的可检查结果。后续追加上游文件时，在对应小节的表格里补行。

## 固定基线

| 项 | 值 |
| --- | --- |
| 上游仓库 | `gitcode.com/opengauss/openGauss-connector-adonet` |
| 本地 checkout | `D:\Code\dotnet-source\openGauss-connector-adonet`（相对本仓库 `../../dotnet-source/openGauss-connector-adonet`） |
| 提交 | `ba5e8ba629b0d660d095da6d8af94f487a00730c`（2023-11-07） |
| 导入范围 | `src/` 全部七个项目与 `src/Shared`、`test/` 六个项目、`LICENSE`、`openGauss.png`、`.editorconfig`、`.gitattributes` |
| 未导入 | 上游 `OpenGauss.sln`（改用 `OpenGaussConnector.slnx`）、`README.md`/`README.en.md`（根级说明按本项目重写时再补）、`OpenGauss.snk`（改用本项目密钥）、上游根 `Directory.Build.props`/`Directory.Packages.props`/`global.json`（按本项目方案重建） |

## 路径映射

| 上游 | 本项目 |
| --- | --- |
| `src/OpenGauss.NET/` | `src/OpenGaussConnector/` |
| `src/OpenGauss.SourceGenerators/` | `src/OpenGaussConnector.SourceGenerators/` |
| `src/OpenGauss.GeoJSON.NET/` | `src/OpenGaussConnector.GeoJSON/` |
| `src/OpenGauss.Json.NET/` | `src/OpenGaussConnector.Json.NET/` |
| `src/OpenGauss.NetTopologySuite/` | `src/OpenGaussConnector.NetTopologySuite/` |
| `src/OpenGauss.NodaTime.NET/` | `src/OpenGaussConnector.NodaTime/` |
| `src/OpenGauss.OpenTelemetry.NET/` | `src/OpenGaussConnector.OpenTelemetry/` |
| `src/Shared/` | `src/Shared/`（原样保留，由 `src/Directory.Build.props` 编译进各产品项目） |
| `test/` | `tests/` |
| `test/OpenGauss.Tests/` | `tests/OpenGaussConnector.Tests/` |
| `test/OpenGauss.PluginTests/` | `tests/OpenGaussConnector.PluginTests/` |
| `test/OpenGauss.NodaTime.Tests/` | `tests/OpenGaussConnector.NodaTime.Tests/` |
| `test/OpenGauss.Specification.Tests/` | `tests/OpenGaussConnector.Specification.Tests/` |
| `test/OpenGauss.TrimmingTests/` | `tests/OpenGaussConnector.TrimmingTests/` |
| `test/OpenGauss.Benchmarks/` | `tests/OpenGaussConnector.Benchmarks/` |

## 机械重命名

按以下顺序做全局替换（长串在前，避免前缀互相吞噬）。只改标识符、命名空间、项目引用与文档名，不改协议、SQL 与公共类型名。

| 上游 | 本项目 |
| --- | --- |
| `OpenGauss.SourceGenerators` | `OpenGaussConnector.SourceGenerators` |
| `OpenGauss.GeoJSON.NET` | `OpenGaussConnector.GeoJSON` |
| `OpenGauss.Json.NET` | `OpenGaussConnector.Json.NET` |
| `OpenGauss.NetTopologySuite` | `OpenGaussConnector.NetTopologySuite` |
| `OpenGauss.NodaTime.NET` | `OpenGaussConnector.NodaTime` |
| `OpenGauss.OpenTelemetry.NET` | `OpenGaussConnector.OpenTelemetry` |
| `OpenGauss.Tests` 等五个测试项目名 | `OpenGaussConnector.*` |
| `OpenGauss.NET`（命名空间、程序集、项目引用、文档） | `OpenGaussConnector` |

保留不动的公共类型名：`OpenGaussConnection`、`OpenGaussCommand`、`OpenGaussDataReader`、`OpenGaussTransaction`、`OpenGaussParameter`、`OpenGaussParameterCollection`、`OpenGaussConnectionStringBuilder`、`OpenGaussBatch`、`OpenGaussFactory`、`PostgresException`、`OpenGaussDbType` 等。

## 与上游的显式偏离

除重命名外，导入阶段做了下述改动。每项都给出原因与可复核位置，便于按上游快照逐项对照。

| 编号 | 改动 | 原因 |
| --- | --- | --- |
| D01 | 内部类 `OpenGaussConnector`（原 `OpenGauss.NET.Internal`）改名 `PhysicalConnector`，文件同步改名为 `Internal/PhysicalConnector.cs`、`.Auth.cs`、`.FrontendMessages.cs` | 根命名空间为 `OpenGaussConnector`，同名公共类型在 `namespace OpenGaussConnector` 内被解析为命名空间（CS0118），无法保留原名。类型位于 `Internal` 命名空间，非 ADO.NET 公共入口 |
| D02 | 删除各文件的 `using OpenGauss;` | 上游根命名空间是带点的 `OpenGauss.NET`，`using OpenGauss;` 实际导入其父命名空间且恒为冗余；根命名空间改为单段后该指令无法解析（CS0246） |
| D03 | `OpenGaussConnectionStringBuilder.cs` 的 `NET.TargetSessionAttributes` 改为 `OpenGaussConnector.TargetSessionAttributes` | 上游靠"`NET` 是父命名空间 `OpenGauss` 的成员命名空间"这一隐含解析；单段根命名空间下不再成立 |
| D04 | `Properties/AssemblyInfo.cs` 去掉 `[assembly: AllowPartiallyTrustedCallers]`、`[assembly: SecurityRules(SecurityRuleSet.Level1)]`，`InternalsVisibleTo` 移入项目文件的 `InternalsVisibleTo` 项 | 前两者是 .NET Framework CAS 语义，在五组目标上不适用；友元程序集改用项目公钥，随签名身份生成 |
| D05 | 强命名改用本项目 `OpenGaussConnector.snk`，公钥以 `$(OpenGaussConnectorPublicKey)` 传给 `InternalsVisibleTo` | 设计要求的"签名使用项目自己的身份"，不沿用上游密钥 |
| D06 | `PhysicalConnector.Auth.cs` 的 `Hi` 在 `NET8_0_OR_GREATER` 上改用 `Rfc2898DeriveBytes.Pbkdf2(password, salt, iteration, HashAlgorithmName.SHA1, 32)` | 旧构造函数在 .NET 10 标记为过时（SYSLIB0060）。算法与参数与原实现一致（PBKDF2-HMAC-SHA1，32 字节），认证行为不变 |
| D07 | `PhysicalConnector.cs` 在 `NET9_0_OR_GREATER` 上用 `X509CertificateLoader.LoadCertificateFromFile`/`LoadCertificate`/`LoadPkcs12`/`LoadPkcs12FromFile` 替换过时的 `X509Certificate2` 构造（SYSLIB0057） | .NET 9 起旧构造过时；loader 是官方替代路径，证书选择、信任链与 PEM/PKCS#12 语义保持不变 |
| D08 | `OpenGaussException`/`PostgresException` 的二进制序列化成员用 `#pragma warning disable SYSLIB0051`（及 `CS0672`）局部抑制 | 保留五组资产一致的公共 API 与 netstandard 上的序列化支持；成员本身在 .NET 8+ 已过时但可用 |
| D09 | 源生成器项目的 `Microsoft.CodeAnalysis.Analyzers` 引用与两份空 `AnalyzerReleases.*.md` 删除；`Microsoft.CodeAnalysis.CSharp` 升到 4.8.0，`Scriban.Signed` 升到 7.6.1 | 生成器是内部构建项目，不按可分发 analyzer 包发布（RS1036/RS2008 只适用于后者）；Scriban 5.0.0 有已知漏洞告警（NU1902/1903/1904） |
| D10 | `TypeHandlerSourceGenerator.cs` 对 `UsingDirectiveSyntax.Name` 增加空判断 | 新版 Roslyn 的可空注解使原写法触发 CS8602（本项目开 `TreatWarningsAsErrors`） |
| D11 | 未设置 `global.json` | 按项目负责人要求不固定 SDK 版本，直接使用已安装 SDK（本次验证为 10.0.403）。设计文档中的相关条目随此项调整 |
| D12 | BCL 补充包版本：`System.Text.Json`、`System.Threading.Channels`、`System.Collections.Immutable`、`System.Diagnostics.DiagnosticSource` 取 10.0.0，`System.Runtime.CompilerServices.Unsafe` 取 6.1.2，`Microsoft.Bcl.HashCode` 取 1.1.1 | 上游 6.0.0 组合触发 NU1903（System.Text.Json 已知漏洞告警）。取值对应本次验证的 .NET 10 宿主，netstandard 两组资产仍可恢复 |
| D13 | 四个扩展项目保留上游的 `Microsoft.CodeAnalysis.PublicApiAnalyzers`（版本移入 CPM）与 `PublicAPI.*.txt` 声明文件；核心不引入该分析器 | 扩展包继续按声明文件跟踪公共 API（B06）；核心没有上游声明文件，整份 ADO.NET 公共面应由发布阶段的 Package Validation 校验，避免一次性生成数千行声明 |
| D14 | 修正重命名误伤的限定名：`using X = PhysicalConnector.StatementType`、`PhysicalConnector.Internal.*`、`PhysicalConnector.Json.NET`、`NET.Schema.*` 等恢复为 `OpenGaussConnector.*`；`using OpenGauss;` 之外所有 `NET.<Type/命名空间>` 的父命名空间写法改为完整命名空间 | 全局替换按 token 处理，会命中"命名空间限定写法"里的根命名空间；这类位置出现在命名空间别名、基类引用、泛型参数和注释中。由 `eng/Verify-UpstreamFidelity.ps1` 逐文件比对发现并修复 |
| D15 | 作者、公司、版权、许可证集中到根 `Directory.Build.props`，删除各项目文件里的 `<Authors>`（上游为 `Microestc`） | 通用包元数据只应有一处定义；各项目仅保留包描述与标签等包特有信息 |

## 导入验证

| 日期 | 检查 | 结果 |
| --- | --- | --- |
| 2026-11-24 | `dotnet restore` + `dotnet build OpenGaussConnector.slnx -c Release` | 成功，0 警告 0 错误，生成 `netstandard2.0`、`netstandard2.1`、`net8.0`、`net9.0`、`net10.0` 五份 `OpenGaussConnector.dll` |
| 2026-11-24 | 生成器产物（`-p:EmitGeneratedFiles=true`） | 生成 `OpenGaussConnectionStringBuilder.Generated.cs` 与 43 个类型处理器分部类，命名空间均为 `OpenGaussConnector` |
| 2026-11-24 | 离线冒烟（`artifacts/tmp/smoke`，不入库） | 连接串默认值/往返/大小写关键字、`OpenGaussFactory.Instance`、`DbProviderFactories.GetFactory("OpenGaussConnector")`、未打开连接状态、参数集合、`DbBatch`、命名空间断言全部通过 |
| 2026-11-24 | `eng/Verify-UpstreamFidelity.ps1` | 逐文件比对 424 个上游 `.cs` 文件，除下表 10 处记录偏离外无非预期差异；该脚本用于后续追加上游文件后的回归检查 |
| 待接入 | 测试项目迁移、五组资产强制加载与包消费 | 见 [开发进度](opengauss-connector-progress.md) |

`eng/Verify-UpstreamFidelity.ps1` 把两侧都折叠成同一套占位名（项目/命名空间 → `NS.*`，内部连接类型 → `CONNTYPE`）后比较内容，因此任何非重命名改动都会被报出来。本地上游快照缺失时先按[参考资料](references.md#本地源码)的路径检出对应提交。
