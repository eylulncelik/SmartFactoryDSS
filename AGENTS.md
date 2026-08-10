# SmartFactoryDSS contributor notes

- Keep the V1 scope limited to dashboard, machine management, maintenance management, AI failure-risk prediction, and prediction history.
- The web application is a single ASP.NET Core MVC project; do not introduce additional .NET layers without a clear need.
- The CSV dataset is only for machine-learning training. Application data belongs in SQL Server.
- Never use `MachineId`, `Timestamp`, `FailureType`, or `FailureWithin24h` as ML features. `FailureWithin24h` is the target.
- Do not commit credentials, local databases, Python virtual environments, generated model files, or build output.
