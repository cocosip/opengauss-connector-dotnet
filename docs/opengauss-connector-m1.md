# M1 开工清单

2026-11-24 · 适用于首次移植和离线验收

M1 从固定的 ADO.NET 快照开始。用同一份源码生成七个 NuGet 包，每包提供 `netstandard2.0`、`netstandard2.1`、`net8.0`、`net9.0`、`net10.0` 五组资产。工作按下表推进，联机环境可并行准备。 [包命名与公共 API](opengauss-connector-design.md) · [功能编号](opengauss-connector-feature-matrix.md)

## 导入与移植顺序

| 步骤 | 工作 | 可检查的结果 |
| --- | --- | --- |
| 1. 固定导入 | 从 `ba5e8ba629b0d660d095da6d8af94f487a00730c` 导入核心、五个扩展、生成器、测试和基准项目。保留模板、测试数据、公共 API 清单、许可证及适用的源文件头。检查上游 MSBuild 文件和打包素材中的引用，按新方案重建配置。 | 导入记录包含仓库、提交、上游到本项目的路径映射及未导入项的理由。可用[参考资料](references.md#本地源码)的 `git ls-tree` 对照。 |
| 2. 调整项目身份和构建 | 建立 `OpenGaussConnector.slnx`、`global.json`、`Directory.Build.props` 和 `Directory.Packages.props`。将旧 `test/` 目录移至 `tests/`，修复项目、资源和工具引用。区分 `PackageId`、`AssemblyName`、实际 C# 命名空间及工厂 invariant。 | 现有公共类型可通过新命名空间使用。逐资产检查 `OpenGaussFactory.Instance`，并按宿主验证工厂注册。记录旧包共存结果以及强命名与程序集迁移决定。对应 B01–B03。 |
| 3. 恢复生成器与五目标编译 | 保留连接串和类型处理的两份 `.snbtxt` 模板，更新嵌入资源名、生成的命名空间和类型引用。保留生成器的 Analyzer 引用方式，验证 Scriban 等仅用于构建的依赖可由编译器加载。清理旧 TFM 条件并按目标核对公开 API。 | 干净构建能生成连接串和类型处理代码，连接串生成属性与运行时关键字一致。生成器、测试、基准和构建依赖不出现在运行时资产或包依赖中。对应 B01、B05、B06。 |
| 4. 迁移核心、DI 与扩展 API | 先恢复上游的连接、执行、批量和类型映射行为，再添加现代 TFM 的 DataSource 与 Builder、DI 的瞬态连接和普通／键控单例数据源。迁移 GeoJSON、Json.NET、NetTopologySuite、NodaTime、OpenTelemetry 的注册和映射。 | 有独立的核心消费用例。按资产验证 DI 注册与释放，按包验证扩展注册、映射隔离和 OpenTelemetry 监听。对应 B04、B05、B08、X01–X05。 |
| 5. 接通测试与打包 | 将核心、插件、NodaTime、规范和裁剪项目接入 SLNX，保留基准的独立运行入口。启用两组 GIS 用例并核对发现数和实际执行数。运行离线测试，再使用本地包源验收。 | 留存 SDK、构建、测试、包清单与消费结果。按环境记录待执行的联机用例。对应 B06、B09。 |

导入、机械重命名与构建适配、行为变更建议各自提交，便于用上游快照逐项核对。上游根 `Directory.Build.props` 启用了强命名，核心和 NodaTime 的 `Properties/AssemblyInfo.cs` 含带公钥的 `InternalsVisibleTo`。调整签名时同步更新友元程序集名称及公钥，并检查测试引用和打包程序集身份。项目签名基线应使用本项目身份。

现代宿主通过 `DbProviderFactories.RegisterFactory` 显式注册，并验证 `GetFactory("OpenGaussConnector")` 返回预期工厂。`RegisterFactory` 从 .NET Standard 2.1 起可用。Standard 2.0 编译阶段用 `OpenGaussFactory.Instance` 验证工厂 API，.NET Framework 4.8 的候选消费用例另外验证宿主配置中的 `<system.data>` 工厂条目。 [Microsoft 工厂注册资料](references.md#net-与-nuget-官方资料)

## 首次构建和测试入口

在仓库根目录完成上述项目和测试配置后，使用以下命令。每一步成功后再执行下一步。新建解决方案时使用 `dotnet new sln --name OpenGaussConnector --format slnx`。仓库不设 `global.json`，直接使用开发机已安装的 .NET 10 SDK（本次为 `10.0.403`），实际版本记入验收记录。 [SLNX、CPM 和 Package Validation](references.md#net-与-nuget-官方资料)

```powershell
dotnet --info
dotnet sln OpenGaussConnector.slnx list
dotnet restore OpenGaussConnector.slnx
dotnet build OpenGaussConnector.slnx -c Release --no-restore
dotnet test OpenGaussConnector.slnx -c Release --no-build --filter "TestCategory=Offline"
dotnet pack OpenGaussConnector.slnx -c Release --no-build --include-symbols '-p:SymbolPackageFormat=snupkg' -o artifacts/packages
```

沿用上游测试框架，以 NUnit 的 `Category` 属性和 xUnit 的 `Trait("TestCategory", ...)` 标记 `Offline` 或 `Integration`。VSTest 以 `TestCategory` 匹配 NUnit 类别，xUnit 则按指定的 Trait 键匹配。离线路径覆盖连接串、工厂、生成器产物、DI 生命周期和可离线执行的映射、跟踪或合约用例。核对每个预期包含离线用例的项目确实发现并执行了这些用例，并检查测试发现与夹具初始化在离线模式下也不会尝试连接数据库。全量构建包含联机用例，明确需要服务端的 GIS、规范、裁剪或基准用例由联机入口执行。 [Microsoft 测试筛选规则](references.md#net-与-nuget-官方资料)

上游 `TestUtil.cs`、`OpenGaussDbFactoryFixture.cs`、裁剪程序和基准辅助代码使用 `NPGSQL_TEST_DB`，部分入口缺少配置时会采用默认连接串。迁移后，联机入口显式读取测试连接串，缺少配置时记录原因。显式请求联机验收时，缺少必要连接串即报告配置错误。检查 `TestGeoJSONTests` 和 `NetTopologySuiteTest` 对测试发现的影响，分开记录编译、发现、实际执行和环境跳过数量。裁剪及 Native AOT 的发布验收见 B07。

## 七包消费与 M1 完成条件

- [ ] 检查核心、DependencyInjection、GeoJSON、Json.NET、NetTopologySuite、NodaTime 和 OpenTelemetry 共七个 `.nupkg`。逐包核对五组 `lib/<TFM>/`、`.nuspec` 依赖组、README、许可证和对应符号产物。六个扩展包都使用同一发布版本校验核心依赖下限。
- [ ] 从本次生成的本地包源恢复独立消费项目。使用隔离包缓存，确保产物中的七个包 ID 解析到本次生成的包。检查 `project.assets.json` 的编译和运行时资产、实际解析版本，并核对运行时加载的 DLL。DI 和按 TFM 选择的基础依赖分别检查最低版本及选定的兼容补丁版。
- [ ] 用 .NET 8、9、10 宿主分别运行对应现代资产。为两个 Standard 目标分别编译调用方，并在独立消费用例中显式运行相应 Standard DLL。Windows .NET Framework 4.8 作为 `netstandard2.0` 的候选宿主，按实测结果记录覆盖。
- [ ] 补充 Standard 到现代资产的批量 API 绑定用例。用 Standard 类库编译调用 `CreateBatch` 和涉及 shim 的公开签名，再由正常恢复包的现代应用调用该类库，确认 NuGet 选择了现代资产。核对 `DbBatch` 等同名类型所处的程序集。若绑定失败，先调整跨 TFM API 方案，并在发布前记录受影响的签名和迁移方式。
- [ ] 记录七包的离线结果、资产选择、依赖版本与迁移兼容结论。真实服务端功能和五组资产的联机冒烟在 M2/M3 记录，按[开发进度](opengauss-connector-progress.md#验收记录)回填结果。

## M2 环境输入

M1 可在未配置 openGauss 服务端时开始。M2 需要可访问的 6.0.6 LTS 环境，先建立单节点测试数据库。按可实际建库的 A、B、PG 模式记录版本与构建号、发行形态、连接后的兼容模式、认证配置和测试账号权限。TLS、主备和 PostGIS 用例再记录证书、拓扑、数据库扩展版本与适用模式。7.0.0 LTS 使用同一记录格式进入 M3。用户实际部署版本和升级历史待采集，验收结果按实际环境填写。 [联机执行顺序](opengauss-connector-progress.md#联机执行顺序)
