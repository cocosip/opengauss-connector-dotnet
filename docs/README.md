# OpenGaussConnector documentation

| Document | Purpose |
| --- | --- |
| [Driver design](opengauss-connector-design.md) | Porting strategy, packages and frameworks, protocol, data types, acceptance and release |
| [Feature matrix](opengauss-connector-feature-matrix.md) | Item-by-item comparison of the old ADO.NET driver, JDBC and server evidence, with priority and verification conditions |
| [Development progress](opengauss-connector-progress.md) | Current state, milestones, task dependencies and the acceptance log format |
| [M1 starter checklist](opengauss-connector-m1.md) | Upstream import order, build entry points, test environment and acceptance of the seven packages |
| [Import record](opengauss-connector-import.md) | Pinned upstream commit, path mapping, mechanical renames and recorded deviations from upstream |
| [References](references.md) | Source snapshots, relative paths and versioned official material |

The design baseline was updated on 2026-11-24. The first release contains seven packages — core, dependency injection, GeoJSON, Json.NET, NetTopologySuite, NodaTime and OpenTelemetry — each built for five target frameworks. JDBC release differences and the server versions to verify are in the [feature matrix](opengauss-connector-feature-matrix.md). Implementation and acceptance status is in [development progress](opengauss-connector-progress.md).
