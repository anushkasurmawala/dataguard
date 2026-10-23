# DataGuard

## Overview

DataGuard is a prototype full-stack application for comparing reference and incoming datasets and identifying data-quality changes and feature-distribution drift. Its deliberately small scope is suitable for a short, three-week .NET internship project. It is not a production monitoring platform.

Upload a baseline CSV and a current CSV, inspect their profiles, then compare them. All displayed results come from C# calculations over the uploaded files. The “Try the sample datasets” button uploads actual synthetic CSV files to the same API used for your files; it does not load precomputed results.

## Technology Stack

- Angular 19, TypeScript, HTML, CSS
- ASP.NET Core 8 Web API and C#
- Entity Framework Core 8 with Npgsql
- PostgreSQL 16 (Docker setup included)
- Chart.js 4
- CsvHelper for quoted CSV parsing

## Features

- CSV dataset upload, validation, and saved profiles
- Row/column counts, exact duplicate rows, missing cells
- Per-feature type inference, missingness, distinct values
- Numerical mean, median, population standard deviation, min/max, Q1/Q3
- Categorical frequencies and most frequent category
- Added, removed, and type-changed columns
- Missing-value differences in percentage points
- IQR outlier counts and percentages
- PSI drift analysis and baseline/current distribution charts
- Status filtering and feature inspection
- REST endpoints to retrieve persisted results

## Architecture

```text
Angular frontend (localhost:4200)
        |
        v
ASP.NET Core REST API (localhost:5080)
        |
        v
C# Dataset / Profiling / Comparison services
        |
        v
Entity Framework Core
        |
        v
PostgreSQL
```

```text
dataguard/
├── frontend/src/             # Upload, profile, dashboard and feature view
├── backend/DataGuard.Api/
│   ├── Controllers/          # HTTP routes
│   ├── Services/             # CSV processing and comparison orchestration
│   ├── Statistics/           # Quantiles, IQR, PSI
│   ├── Models/               # Persistence entities
│   ├── DTOs/                 # API contracts
│   ├── Data/                 # EF context and design-time factory
│   └── Migrations/           # Initial PostgreSQL schema
├── sample-data/              # Three synthetic CSVs
├── tests/DataGuard.Tests/    # Small executable C# unit-test suite
├── compose.yaml
└── DataGuard.sln
```

Controllers handle HTTP, services handle processing, and statistics classes calculate results. There is no authentication, scheduler, alerting, microservice layer, or cloud deployment.

### Persistence

The small database uses two EF entities: `Datasets` and `Comparisons`. Dataset records contain filename, role, upload time, file path, and a complete profile stored as PostgreSQL JSONB. Comparisons have foreign keys to both datasets, creation time, and the complete comparison snapshot in JSONB (feature comparisons, schema changes, saved baseline boundaries, histogram proportions). This avoids multiple child tables for a prototype while retaining real PostgreSQL persistence and referential integrity. File contents remain in the backend's ignored `uploads/` directory. Keep this folder together with database backups if preserving results.

## Running Locally

Prerequisites: .NET 8 SDK (8.0.400 or newer 8.x feature band), Node.js 22, npm, and Docker Compose or a local PostgreSQL server. Run the following from the `dataguard` directory.

### 1. Start PostgreSQL

```sh
docker compose up -d --wait
```

This starts PostgreSQL 16 on loopback port 5432 with database/user/password `dataguard`. Credentials are only development defaults. The named Docker volume preserves the database across restarts.

Alternatively, use your own PostgreSQL instance and create a role/database:

```sql
CREATE USER dataguard WITH PASSWORD 'dataguard';
CREATE DATABASE dataguard OWNER dataguard;
```

If port 5432 is already used, change the Compose host port and set the matching connection string. The optional environment override applies to both migration and API commands:

```sh
export ConnectionStrings__DataGuard='Host=localhost;Port=5432;Database=dataguard;Username=dataguard;Password=dataguard'
```

### 2. Restore, build, migrate, and check calculations

```sh
dotnet tool restore
dotnet restore DataGuard.sln
dotnet build DataGuard.sln --no-restore
dotnet ef database update --project backend/DataGuard.Api
dotnet run --project tests/DataGuard.Tests
```

The lightweight unit-test executable prints PASS/FAIL and exits nonzero on failure. It intentionally uses no test framework. Run it with `dotnet run`, rather than `dotnet test`.

### 3. Start the API

```sh
dotnet run --project backend/DataGuard.Api --no-launch-profile --urls http://127.0.0.1:5080
```

Keep this terminal running. `GET http://127.0.0.1:5080/api/health` checks the HTTP process; it does not claim database readiness. App-settings file watching is disabled because configuration hot reload is unnecessary here and can hang in restricted environments.

### 4. Start Angular in a second terminal

```sh
cd frontend
npm ci
npm start
```

Open **http://127.0.0.1:4200**. The Angular development proxy forwards `/api` to port 5080. No API keys are needed.

For a frontend build:

```sh
npm run build
```

The bundle is emitted to `frontend/dist/dataguard/browser/`. Angular's persistent build cache is disabled for compatibility with native cache libraries on this development Mac.

### 5. Demonstrate the workflow

1. Click **Try the sample datasets**, or select `sample-data/customer_baseline.csv` and `sample-data/customer_current.csv` manually.
2. Expand either dataset profile to inspect missingness, duplicate rows, numerical statistics, and category frequencies.
3. Click **Compare datasets**.
4. Review schema changes, calculated PSI classifications, and missingness changes.
5. Select a feature to view its statistics, outliers, and shared-bin histogram.
6. Use Stable / Moderate / Significant filters to narrow the table.
7. For an additional schema example, replace the current upload with `schema_change_current.csv`. It removes `age`, adds `customer_segment`, and changes income to string.

Sample data is synthetic and includes higher income values, more missing income, unusual balances, relatively similar ages, a new region category, and duplicated complete rows. Dashboard values are never hard-coded. Sample files are served by Angular as assets and uploaded through the normal API.

### API smoke test

With the API and PostgreSQL running, execute from the repository root:

```sh
python3 tests/api_smoke.py
```

This standard-library HTTP test uploads the sample files, creates/retrieves comparisons, checks schema and quality results, verifies errors for malformed CSV, and confirms saved results can be retrieved. Python is only an HTTP test client; all application parsing, profiling, statistics, and drift logic runs in C#.

### REST API

- `POST /api/datasets/baseline` — multipart form field `file`
- `POST /api/datasets/current` — multipart form field `file`
- `GET /api/datasets/{id}/profile`
- `POST /api/comparisons` — JSON with `baselineDatasetId` and `currentDatasetId`
- `GET /api/comparisons/{id}`
- `GET /api/comparisons/{id}/features`
- `GET /api/health`

Validation returns HTTP 400; unknown dataset IDs return 404. Unexpected failures return a generic 500 message with details in local server logs. Profiles and comparisons are persisted; the UI keeps the active selections in memory, so refreshing starts a new workflow. Existing results remain retrievable via the API.

## Calculation Details

### CSV and types

Comma-delimited UTF-8 CSV with a header row is supported, including quoted commas, escaped quotes, and multiline fields. Headers must be nonempty and unique (case-sensitive); row widths must match. Limits: 20 MB, 100,000 records, and 200 columns. Blank lines are ignored. Fields are trimmed. Only blank/whitespace fields count as missing; literal `NA`, `null`, or `?` remain strings. No missing values are imputed.

Types are inferred from all non-missing values: finite invariant-culture numbers, booleans, ISO `yyyy-MM-dd` dates, or strings. All-missing features are `unknown`. A single nonnumeric value makes a numerical-looking feature a string; this is surfaced as a schema type change when appropriate. Headers are matched exactly. This is basic type inference, not schema enforcement.

Duplicate count means records beyond the first occurrence of the same complete trimmed row, including ID fields. Missing percentages use all rows. Overall missingness uses all cells. Numeric statistics, outliers, and PSI exclude missing values.

Quantiles use linear interpolation at `(n - 1) × p`. Standard deviation is the population standard deviation. Outliers lie strictly outside `[Q1 - 1.5 × IQR, Q3 + 1.5 × IQR]`, using each dataset's own quartiles. The outlier percentage denominator is its non-missing numerical count. Outliers are unusual observations, not automatically errors.

### PSI

1. Sort the non-missing baseline numerical values.
2. Derive up to nine distinct interior decile cut points (approximately ten bins). Repeated boundaries are collapsed.
3. Apply those **same baseline cut points** to both datasets. Bins are right-closed; the first/last extend to negative/positive infinity so new extremes are retained.
4. Calculate the proportion of non-missing observations in each bin.
5. Smooth each proportion with epsilon `0.000001`, then normalize: `(p + epsilon) / (1 + binCount × epsilon)`.
6. Sum `(currentProportion - baselineProportion) × ln(currentProportion / baselineProportion)`.

Constant baselines use three bins: below, equal to, and above the baseline value. Low-cardinality numeric features may have fewer bins. The API persists the baseline boundaries and bin proportions. Charts display raw percentages, whereas PSI uses the smoothed proportions. Missingness is compared separately.

- PSI < 0.10: **Stable**
- 0.10 ≤ PSI < 0.25: **Moderate Change**
- PSI ≥ 0.25: **Significant Change**

These are commonly used monitoring conventions, not universal scientific rules or statistical significance tests. The implementation has no minimum sample-size rule, so small-sample results need particular care. `id`, names ending `_id` (case insensitive), and names ending `Id` are excluded from PSI as ID-like features. Other numerical identifiers may still require interpretation. Categorical/date/boolean features and features with different inferred types are profiled but receive no PSI.

## Limitations

- Internship prototype, intended for a small demonstration within a three-week project scope
- CSV files only; processing is in memory and bounded by upload/row/column limits
- Primarily numerical feature-distribution drift; categorical frequencies are descriptive
- Basic type inference and ID heuristic; no semantic schema validation
- PSI thresholds are conventions and results depend on baseline bins and sample size
- Data drift does **not** automatically imply model-performance degradation; no model predictions or outcomes are evaluated
- No authentication, retention policy, concurrency tuning, deployment hardening, or production monitoring features
- Not intended as a production, enterprise-scale, or commercial monitoring platform

## Development phases

A suggested short-internship sequence: scaffold API/Angular/database; CSV upload; profiles; schema and missingness comparisons; IQR; PSI; REST comparison persistence; dashboard; charts; samples and tests; documentation and cleanup. Build and check relevant functionality after each phase. This sequence describes project scope, not a claim about a historical internship schedule.

## Verification

See `VERIFICATION.md` for checks actually performed in the supplied development environment and their limits.
