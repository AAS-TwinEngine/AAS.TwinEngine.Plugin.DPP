# AAS.TwinEngine.Plugin.DPP.PlaywrightTests

This project contains Playwright-based API tests translated from the Bruno collection in DataEngine `example/apiCollection`.

Prerequisites
- .NET SDK (7.0+)
- `playwright` CLI (optional; see below)

Quick start

1. Set the DataEngine base URL (defaults to `http://localhost:8080`):

```powershell
$env:DATAENGINE_BASE_URL = "http://localhost:8080"
```

2. Restore packages and run tests:

```powershell
dotnet restore
dotnet test
```

3. (Optional) Install Playwright browsers if needed:

```powershell
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

Project layout
- `AasRegistry/` — Tests for AAS Registry endpoints
- `AasRepository/` — Tests for AAS Repository endpoints
- `SubmodelRegistry/` — Tests for Submodel Registry endpoints
- `SubmodelRepository/` — Tests for Submodel Repository endpoints

Notes
- The test base class reads the target base URL from the `DATAENGINE_DPPPLUGIN_BASE_URL` environment variable.
- Replace placeholder tests with tests converted from the Bruno collection.
