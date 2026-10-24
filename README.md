# BankAccountPlaywright — C# + Playwright + MSTest

A Visual Studio 2022 solution with an ASP.NET Core 8 bank account web application and browser-based automated tests.

## Requirements
- Visual Studio 2022 with ASP.NET and web development workload
- .NET 8 SDK
- Internet access for the first NuGet restore and Chromium installation

## Run the app
1. Open `BankAccountPlaywright.sln`.
2. Set `BankAccountWeb` as Startup Project.
3. Press Ctrl+F5. Open the URL printed in the console (or use `dotnet run --project BankAccountWeb --urls http://127.0.0.1:5187`).

## Install Playwright Chromium (first time only)
Open a terminal at the solution root:

```powershell
dotnet restore
dotnet build
pwsh BankAccountPlaywrightTests/bin/Debug/net8.0/playwright.ps1 install chromium
```

If `pwsh` is unavailable, install PowerShell 7 or run the generated `playwright.ps1` with Windows PowerShell using a policy permitted by your organization. You can alternatively run `playwright install chromium` through the Playwright CLI appropriate to your installation.

## Run tests
- Visual Studio: **Test > Test Explorer > Run All Tests**.
- CLI: `dotnet test`
- Smoke tests only: `dotnet test --filter TestCategory=Smoke`

The test class starts its own web server on port 5187, resets the in-memory account to 100 PLN before each test, and stops the server when tests finish. **Stop any manually launched server on port 5187 before running tests.** Do not run this test assembly concurrently in multiple processes.

## Test cases
- Smoke: home page and initial balance
- Smoke: deposit increases balance
- Withdrawal decreases balance
- Insufficient funds displays error and preserves balance
- Invalid amount (0 and -5) displays validation
- Balance persists across page reload

## Notes
- This is an educational demo: balance is stored in memory, not in a database, and is reset after server restart.
- The API reset endpoint is for test isolation only and must not be exposed in a production banking application.
- The test suite has not been executed in this environment (no .NET SDK available).
