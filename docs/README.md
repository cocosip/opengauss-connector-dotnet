# OpenGaussConnector 文档

| 文档 | 用途 |
| --- | --- |
| [驱动设计](opengauss-connector-design.md) | 移植策略、包与框架、协议、数据类型、验收和发布 |
| [功能矩阵](opengauss-connector-feature-matrix.md) | 逐项比对旧 ADO.NET、JDBC 和服务端依据，给出优先级与验证条件 |
| [开发进度](opengauss-connector-progress.md) | 当前状态、里程碑、任务依赖与验收记录格式 |
| [M1 开工清单](opengauss-connector-m1.md) | 上游导入顺序、构建入口、测试环境和七包验收 |
| [导入记录](opengauss-connector-import.md) | 上游固定提交、路径映射、机械重命名和与上游的显式偏离 |
| [参考资料](references.md) | 源码快照、相对路径和版本化官方资料 |

2026-11-24 更新设计基线。首发包含核心、依赖注入、GeoJSON、Json.NET、NetTopologySuite、NodaTime 和 OpenTelemetry 七个包，各提供五组 TFM。JDBC 发布差异与拟验证的服务端版本见 [功能矩阵](opengauss-connector-feature-matrix.md)。实现与验收状态见 [开发进度](opengauss-connector-progress.md)。
