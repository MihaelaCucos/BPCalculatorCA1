using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using BPCalculator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBomber.Contracts;
using NBomber.CSharp;

namespace BPCalculator.PerformanceTests;

[TestClass]
[TestCategory("Performance")]
public sealed class CalculatorPerformanceTests
{
    private const int VirtualUsers = 10;
    private static readonly TimeSpan LoadDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WarmUpDuration = TimeSpan.FromSeconds(5);

    [TestMethod]
    public async Task ProductionHost_MeetsBaselineForPageAndFormSubmissions()
    {
        using var host = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureWebHost(webBuilder => webBuilder
                .UseEnvironment(Environments.Production)
                .UseUrls("http://127.0.0.1:0"))
            .Build();

        await host.StartAsync();

        var address = host.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?
            .Addresses.Single()
            ?? throw new InvalidOperationException("The web host did not publish a listening address.");

        using var client = new HttpClient(new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true
        })
        {
            BaseAddress = new Uri(address)
        };

        using var formPage = await client.GetAsync("/");
        formPage.EnsureSuccessStatusCode();
        var formHtml = await formPage.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(
            formHtml,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");
        Assert.IsTrue(tokenMatch.Success, "The form should contain an anti-forgery token.");
        var antiForgeryToken = tokenMatch.Groups[1].Value;

        var scenarios = new[]
        {
            BuildScenario("valid_reading_submission", () =>
                SubmitReadingAsync(client, antiForgeryToken, 120, 79, "Pre-High Blood Pressure")),
            BuildScenario("invalid_reading_submission", () =>
                SubmitReadingAsync(
                    client,
                    antiForgeryToken,
                    80,
                    80,
                    "Systolic must be greater than Diastolic",
                    "Pre-High Blood Pressure"))
        };

        var result = NBomberRunner.RegisterScenarios(scenarios).Run();
        var scenarioSummary = string.Join(
            Environment.NewLine,
            result.ScenarioStats.Select(stats =>
                $"{stats.ScenarioName}: {stats.Ok.Request.RPS:F1} requests/sec, "
                + $"p95 {stats.Ok.Latency.Percent95} ms, "
                + $"{stats.Fail.Request.Count} failed requests"));

        Assert.IsFalse(
            result.Thresholds.Any(threshold => threshold.IsFailed),
            $"One or more performance thresholds failed:{Environment.NewLine}{scenarioSummary}");
    }

    private static ScenarioProps BuildScenario(string name, Func<Task<bool>> operation)
    {
        return Scenario.Create(name, async _ =>
            {
                var succeeded = await operation();
                return succeeded ? Response.Ok(statusCode: "200") : Response.Fail(statusCode: "500");
            })
            .WithWarmUpDuration(WarmUpDuration)
            .WithLoadSimulations(Simulation.KeepConstant(
                copies: VirtualUsers,
                during: LoadDuration))
            .WithThresholds(
                Threshold.Create(stats => stats.Fail.Request.Count == 0),
                Threshold.Create(stats => stats.Ok.Latency.Percent95 <= 1000),
                Threshold.Create(stats => stats.Ok.Request.RPS >= VirtualUsers));
    }

    private static async Task<bool> SubmitReadingAsync(
        HttpClient client,
        string antiForgeryToken,
        int systolic,
        int diastolic,
        string expectedText,
        string? textToExclude = null)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["BP.Systolic"] = systolic.ToString(CultureInfo.InvariantCulture),
            ["BP.Diastolic"] = diastolic.ToString(CultureInfo.InvariantCulture),
            ["__RequestVerificationToken"] = antiForgeryToken
        });
        using var response = await client.PostAsync("/", content);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return false;
        }

        var html = await response.Content.ReadAsStringAsync();
        return html.Contains(expectedText, StringComparison.Ordinal)
            && (textToExclude is null || !html.Contains(textToExclude, StringComparison.Ordinal));
    }
}
