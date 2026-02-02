# StranglerSeamDemo – WinForms => ASP.NET Core “Strangler” Spike

## What this is
A tiny local-only migration spike demonstrating a realistic pattern for modernizing a legacy WinForms feature:
- **Legacy UI (WinForms)** calls a **new ASP.NET Core Web API** through an HTTP seam.
- The API owns the data + rules (EF Core + SQLite).
- This mirrors an incremental “strangler fig” approach: extract one feature slice at a time behind a stable contract.

## Projects
- `StranglerSeamDemo.Api` – .NET 8 Web API
  - GET `/cases?search=&page=&pageSize=` returns paged results
  - PUT `/cases/{id}/status` updates status with validation
  - EF Core + SQLite file DB (`StranglerSeamDemo.db`) seeded with ~30 rows on first run
- `StranglerSeamDemo.LegacyWinForms` – .NET 8 WinForms
  - Search textbox + results grid
  - Status dropdown + update button
  - Handles API downtime gracefully

## Why this maps to real modernization
This spike demonstrates:
- **Seams & Strangler**: WinForms remains in place while a new API implements a carved-out feature slice.
- **Incremental rollout**: you can route only some workflows to the new API (feature flag / config switch).
- **Contract-first thinking**: DTOs + HTTP status codes become the boundary between old/new.
- **Quality & testing**: includes an integration test validating paging/search behavior.
- **Future-ready**: the API can later be hosted in cloud unchanged; WinForms can be replaced gradually by web UI.

## Run it (local)
### 1) Start API
```bash
cd StranglerSeamDemo.Api
dotnet run
```