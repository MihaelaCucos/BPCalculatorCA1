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

namespace BPCalculator.Tests;

[TestClass]
[TestCategory("Integration")]
public sealed class ApplicationHostTests
{
    [TestMethod]
    public async Task DevelopmentHost_ServesHomePrivacyAndErrorPages()
    {
        var (host, client) = await StartHostAsync(Environments.Development);
        using (host)
        using (client)
        {
            Assert.IsTrue((await client.GetAsync("/")).IsSuccessStatusCode);
            Assert.IsTrue((await client.GetAsync("/Privacy")).IsSuccessStatusCode);
            Assert.IsTrue((await client.GetAsync("/Error")).IsSuccessStatusCode);
        }
    }

    [TestMethod]
    public async Task ProductionHost_ServesHomePage()
    {
        var (host, client) = await StartHostAsync(Environments.Production);
        using (host)
        using (client)
        {
            Assert.IsTrue((await client.GetAsync("/")).IsSuccessStatusCode);
        }
    }

    [TestMethod]
    public async Task ValidFormPost_RendersCalculatedCategory()
    {
        var (host, client) = await StartHostAsync(Environments.Development);
        using (host)
        using (client)
        using (var response = await PostReadingAsync(client, 120, 79))
        {
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var html = await response.Content.ReadAsStringAsync();
            StringAssert.Contains(html, "Pre-High Blood Pressure");
        }
    }

    [TestMethod]
    public async Task InvalidSystolicDiastolicOrder_RendersValidationErrorWithoutCategory()
    {
        var (host, client) = await StartHostAsync(Environments.Development);
        using (host)
        using (client)
        using (var response = await PostReadingAsync(client, 80, 80))
        {
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var html = await response.Content.ReadAsStringAsync();
            StringAssert.Contains(html, "Systolic must be greater than Diastolic");
            Assert.IsFalse(html.Contains("Pre-High Blood Pressure", StringComparison.Ordinal));
        }
    }

    [DataTestMethod]
    [DataRow(69, 60, "Invalid Systolic Value")]
    [DataRow(191, 60, "Invalid Systolic Value")]
    [DataRow(120, 39, "Invalid Diastolic Value")]
    [DataRow(120, 101, "Invalid Diastolic Value")]
    public async Task OutOfRangeFormPost_RendersRangeValidationError(
        int systolic,
        int diastolic,
        string expectedError)
    {
        var (host, client) = await StartHostAsync(Environments.Development);
        using (host)
        using (client)
        using (var response = await PostReadingAsync(client, systolic, diastolic))
        {
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

            var html = await response.Content.ReadAsStringAsync();
            StringAssert.Contains(html, expectedError);
        }
    }

    private static async Task<HttpResponseMessage> PostReadingAsync(
        HttpClient client,
        int systolic,
        int diastolic)
    {
        using var formResponse = await client.GetAsync("/");
        var formHtml = await formResponse.Content.ReadAsStringAsync();
        var tokenMatch = Regex.Match(
            formHtml,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""");

        Assert.IsTrue(tokenMatch.Success, "The form should contain an anti-forgery token.");

        var formValues = new Dictionary<string, string>
        {
            ["BP.Systolic"] = systolic.ToString(CultureInfo.InvariantCulture),
            ["BP.Diastolic"] = diastolic.ToString(CultureInfo.InvariantCulture),
            ["__RequestVerificationToken"] = tokenMatch.Groups[1].Value
        };

        return await client.PostAsync("/", new FormUrlEncodedContent(formValues));
    }

    private static async Task<(IHost Host, HttpClient Client)> StartHostAsync(string environment)
    {
        var host = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureWebHost(webBuilder => webBuilder
                .UseEnvironment(environment)
                .UseUrls("http://127.0.0.1:0"))
            .Build();

        await host.StartAsync();

        var address = host.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?
            .Addresses.Single()
            ?? throw new InvalidOperationException("The web host did not publish a listening address.");

        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            UseCookies = true
        };

        return (host, new HttpClient(handler) { BaseAddress = new Uri(address) });
    }
}
