# openGauss ADO.NET 驱动开发进度

更新日期 2026-11-24

## 当前状态

| 事项 | 状态 | 证据或待办 |
| --- | --- | --- |
| 上游调研 | 已完成本轮 | 固定 ADO.NET 与 OB 参考提交，核对 JDBC 四个发布标签及本地开发线、openGauss 发行资料和 DI 包元数据。 [参考资料](references.md) |
| 设计、功能矩阵 | 已走查并更新 | 首发七包、五组 TFM、五个上游扩展的项目清单和功能验收、JDBC 版本差异及驱动内服务端版本选择。 [驱动设计](opengauss-connector-design.md) · [功能矩阵](opengauss-connector-feature-matrix.md) |
| M1 开工清单 | 已补齐 | 记录上游导入、首次构建、测试入口和包消费的执行顺序。 [M1 开工清单](opengauss-connector-m1.md) |
| 上游导入与重命名 | 已完成 | 固定 `ba5e8ba` 快照，七个产品项目与六个测试项目按新路径导入，机械重命名与 13 项显式偏离逐条记录。 [导入记录](opengauss-connector-import.md) |
| 核心源码与源生成器 | 已完成（五目标编译通过） | `OpenGaussConnector` 五组资产构建成功、0 警告 0 错误；连接串生成器与 43 个类型处理器分部类生成正常。 |
| 核心 DataSource/Builder 与 DI 包 | 已完成（五目标编译通过） | `OpenGaussDataSource`/`OpenGaussDataSourceBuilder` 按 .NET 8/9/10 提供并持有独立连接源；DI 包五组资产编译通过，15 项注册、生命周期与释放测试通过。 |
| 五个上游扩展 | 待开始 | 项目文件已导入，尚未接入 SLNX、未编译、未验证。 |
| 测试项目接入与离线测试 | 待开始 | 六个测试项目已导入 `tests/`，项目引用和测试框架版本待修，未接入 SLNX。 |
| 五组资产、包消费、服务端联机 | 待开始 | 未运行打包、包消费或数据库联机测试。 |
| 实际部署版本和升级状态 | 待确认 | 已核对 6.0.6、7.0.0 LTS 的公开资料。用户环境的版本、发行形态与升级情况待采集。 |
| 开发机 SDK 与运行时 | 已盘点 | 检测到 10.0.403 SDK 及 .NET 8/9/10 运行时；仓库不设 `global.json`，直接使用已安装 SDK。CI 使用的精确版本待记录。 |

## 里程碑

| 阶段 | 状态 | 工作与退出条件 |
| --- | --- | --- |
| M0 · 建立设计基线 | 本轮完成 | 记录上游快照、版本化资料、范围与能力验收规则。 |
| M1 · 核心、DI 与五个上游扩展移植 | 进行中 | 核心、生成器、DataSource 与 DI 已完成。剩余五个扩展包（X01–X05）、B09 测试接入、B07/B09 打包与消费。 [M1 开工清单](opengauss-connector-m1.md) |
| M2 · 6.0.6 LTS 核心联机 | 待开始 | 验证 C01、C02、C05、C07、C08、C13 和 E01–E03、E10、T01–T03、T06、T07 的 P0 项。按 A、B、PG 记录结果。在一个已验证模式补齐五组资产联机冒烟，并通过 DI 路径创建连接。 |
| M3 · 7.0.0 LTS 与跨版本回归 | 待开始 | 重复 M2 核心用例与五组资产冒烟，完成 X01–X05 支持环境的逐包功能验收。核对目录、兼容模式、扩展与类型差异。测试版本冲突、版本未知、混合节点及升级后的物理连接重建。 |
| M4 · 优先功能与预发行 | 待开始 | 按测试结果推进混合认证、SM3、主备、批量、COPY、过程、扩展元数据等 P1 项。校验七个包的消费报告及对应的环境能力，记录已知限制与跳过原因。 |
| M5 · 专项增强 | 待评估 | 分别跟踪 oGRAC、ATF、TLCP、全密态和复制输出插件。 |

## M1 分阶段交付记录

每个阶段完成后按"构建 → 测试 → 走查 → 更新本文件 → 提交"闭合。阶段顺序对应 [M1 开工清单](opengauss-connector-m1.md) 的步骤 1–5。

| 阶段 | 范围 | 状态 | 证据 |
| --- | --- | --- | --- |
| A | 仓库骨架（`slnx`、CPM、构建属性）与上游核心导入 | 已完成 | 465 个导入文件；`dotnet build OpenGaussConnector.slnx -c Release` 成功，五份资产 0 警告 0 错误；生成器产物已核对；离线冒烟通过。 [导入记录](opengauss-connector-import.md) |
| B | 核心 DataSource/Builder 与 `OpenGaussConnector.DependencyInjection` | 已完成 | 五组资产 0 警告 0 错误；`tests/OpenGaussConnector.DependencyInjection.Tests` 15 项通过（瞬态连接、别名优先、重复注册、单例释放、键控隔离、数据源连接串固定）。日志工厂接入按[驱动设计](opengauss-connector-design.md)记为 M1 后续项。 |
| C | `OpenGaussConnector.Json.NET` | 待开始 | — |
| D | `OpenGaussConnector.NodaTime` | 待开始 | — |
| E | `OpenGaussConnector.NetTopologySuite` | 待开始 | — |
| F | `OpenGaussConnector.GeoJSON` | 待开始 | — |
| G | `OpenGaussConnector.OpenTelemetry` | 待开始 | — |
| H | 上游测试项目接入与离线测试入口 | 待开始 | — |
| I | 打包、`eng/Verify-Packages.ps1` 与独立消费验证 | 待开始 | — |

### 联机执行顺序

1. 准备 6.0.6 与 7.0.0 的单节点环境，记录发行形态、构建号、可建立的数据库模式、测试账号权限与认证配置。
2. 对两版服务先做启动、SHA256、基本 SQL 与事务冒烟。记录驱动识别的产品版本、启动参数和实际兼容模式，再扩展到参数、类型、版本相关目录、池、取消和 TLS。
3. 建立兼容模式和功能用例的对照表。对失败项用固定版 JDBC、旧 ADO.NET 或服务端 SQL 复现，视场景选择对照。
4. 建立 6.0.6/7.0.0 主备和 TLS 专项所需环境，按实际部署条件安排。推进内容见 [功能矩阵](opengauss-connector-feature-matrix.md)。
5. 在两套环境中核对 PostGIS 和扩展类型可用性，记录实际扩展版本。运行适用的 GIS、Json.NET、NodaTime 和 OpenTelemetry 端到端测试，并记录消费的包和资产。

## 验收记录

每次验证按功能与目标组合记一行。驱动版本识别用例单独记录显式配置、探测值、能力可用性和采用的兼容路径。环境缺失时记录具体缺项。用例执行后标注结果和复现位置，涉及凭据的输出先脱敏。

离线结果记入上面的分阶段交付记录；下表只记需要真实服务端的用例。

| 日期 | 功能 ID | 驱动提交或包版本 | 服务端与构建号 | 数据库模式及扩展 | OS / 宿主 / 资产 TFM | 认证 / TLS / 拓扑 | 结果 | 日志或用例路径 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 待执行 | C01、C02 | 待建立 | 6.0.6 LTS，构建号待记录 | A，扩展待记录 | Windows 或 Linux / .NET 10 / `net10.0` | SHA256 / 待记录 / 单节点 | 待执行 | 待记录 |
