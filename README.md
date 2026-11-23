# bp
Blood Pressure Calculator
ASP.Net Core

## Browser acceptance tests

The Gherkin acceptance scenarios run against the web app in headless Chrome using Selenium WebDriver. Selenium Manager resolves a compatible ChromeDriver automatically; a Chrome/Chromium browser must be installed locally. GitHub Actions installs Chrome for Testing before running the scenarios.

```sh
dotnet build BPCalculator.AcceptanceTests/BPCalculator.AcceptanceTests.csproj --configuration Release
dotnet test BPCalculator.AcceptanceTests/BPCalculator.AcceptanceTests.csproj --configuration Release
```

If Selenium cannot locate your browser automatically, set `CHROME_BIN` to the Chrome/Chromium executable path.

## Performance baseline

The NBomber performance project runs two workload scenarios against the app in `Production` mode: submitting a valid reading and submitting an invalid reading. Each scenario uses 10 concurrent virtual users for 30 seconds after a 5-second warm-up. The page-load-only case is covered by the k6 baseline profile.

The initial acceptance targets are:

- p95 response latency no greater than 1 second
- at least 10 successful requests per second per scenario
- zero failed requests

These are deliberately modest starting targets for a small, stateless calculator. One second is a practical upper bound for a responsive page interaction, while 10 concurrent users and 10 requests per second provide a repeatable baseline without claiming to measure the app's maximum capacity. The workflow hosts the Release app locally with the Production environment settings, so the load does not reach the public Azure app. Results are a CI-runner baseline, not a production capacity guarantee.

Run the load baseline with:

```sh
dotnet test BPCalculator.PerformanceTests/BPCalculator.PerformanceTests.csproj --configuration Release
```

### Grafana k6 profiles

The k6 scripts add three presentation-style workload profiles against a local Production-mode app:

- **Baseline:** 1 virtual user for 30 seconds; checks the calculator page and targets p95 below 1 second.
- **Load:** ramps from 1 to 10 virtual users over 60 seconds, then ramps down; simulates a normal form workload with mostly valid readings and occasional invalid input.
- **Spike:** jumps from 1 to 20 virtual users, holds briefly, then returns to 1; targets p95 below 2 seconds during the short burst.

All profiles require a failed-request rate below 1% and at least 99% successful checks. The limits are starter targets for this small app, not assignment requirements or a production capacity claim. Soak and stress tests are left out because they consume more CI time or intentionally push the app toward failure. The CI jobs run the profiles locally in Production mode; they do not send traffic to Azure or upload results to Grafana Cloud.

Install [Grafana k6](https://grafana.com/docs/k6/latest/set-up/install-k6/) and start the app in one terminal:

```sh
ASPNETCORE_ENVIRONMENT=Production ASPNETCORE_URLS=http://127.0.0.1:5050 dotnet run --project BPCalculator/BPCalculator.csproj --configuration Release
```

Run the profiles from another terminal:

```sh
k6 run --env BASE_URL=http://127.0.0.1:5050 k6/baseline.js
k6 run --env BASE_URL=http://127.0.0.1:5050 k6/load.js
k6 run --env BASE_URL=http://127.0.0.1:5050 k6/spike.js
```
