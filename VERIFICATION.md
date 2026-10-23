# Verification record

Verified locally on 23 October 2026.

## Completed checks

- Built the ASP.NET Core API and C# tests with .NET SDK 8.0.410: **0 warnings, 0 errors**.
- Ran the executable unit suite: **16 passed, 0 failed**. Checks cover identical/shifted/moderate PSI, zero-frequency handling, shared baseline cuts, constant features in both directions, repeated quantiles, IQR outliers, numerical summary values, schema additions/removals/type changes, duplicate rows, missingness, nonfinite inputs, and all-missing features.
- Built the Angular production bundle with Node 22.16.0: **passed**.
- Applied `20261023171051_InitialCreate` through EF Core/Npgsql to the local PostgreSQL-compatible verification runtime. A second update reported the database was already up to date.
- Started ASP.NET Core on port 5080 and Angular on port 4200.
- Used the browser to upload both sample CSVs with the sample-upload button, create a comparison, filter Significant features, return to All, and inspect income statistics and its distribution chart.
- Executed `tests/api_smoke.py`: verified uploads, profile retrieval, duplicate counts, missingness increases, income PSI, stable age, ID exclusion, increased balance outliers, shared-bin totals, added/removed/type-changed schema fields, stored comparison/feature retrieval, quoted/multiline CSV, invalid uploads, constant-baseline downward drift, and invalid dataset-role rejection.
- Saved the calculated browser dashboard in `dashboard.png`.

## Calculated sample observations

These are outputs of the real API, not UI constants:

- Baseline: 1,005 rows, 7 columns, 5 duplicate rows.
- Current: 972 rows, 8 columns, 12 duplicate rows.
- `age`: PSI 0.015477, Stable.
- `annual_income`: PSI 1.205641, Significant; missingness increases by 6.4360 percentage points.
- `account_balance`: PSI 0.088215, Stable; its outlier rate nevertheless increases. Quality metrics and PSI need to be interpreted separately.
- `monthly_transactions`: PSI 0.092136, Stable.
- Added column: `customer_segment`.
- The second schema sample also detects removed `age` and numeric-to-string `annual_income`.

## Environment limits

The installed native PostgreSQL executables had an incompatible CPU architecture. A compatible PostgreSQL binary was obtained in the workspace, but this sandbox denied its shared-memory operation (`shmget`). Native PostgreSQL and the Docker Compose configuration could therefore **not** be exercised here.

For migration, persistence, and end-to-end verification, the unmodified C# API used **PGlite 0.5.11 / pglite-socket 0.2.14**, a WebAssembly PostgreSQL build exposed over the PostgreSQL wire protocol on loopback port 55432. Npgsql and EF Core used their normal PostgreSQL provider. This validates the demonstrated API/database flow but does not replace a run against native PostgreSQL 16. The deliverable defaults to normal PostgreSQL, and the README provides the exact Docker/native setup and migration commands. No PGlite, Node backend, or Python statistics layer was added to the application.

The verification runtime and dependency caches live outside the deliverable under the workspace `work/` directory. Frontend build caching and .NET configuration file watching were disabled to avoid local native-cache crashes and configuration-watcher hangs respectively. The preview servers were stopped after verification; use the README commands to start a new local session.

No production readiness, cloud deployment, model accuracy impact, or enterprise scalability was tested or claimed.
