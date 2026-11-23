using System.Globalization;
using BPCalculator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace BPCalculator.AcceptanceTests.StepDefinitions;

[Binding]
public sealed class BloodPressureSteps
{
    private IHost? _host;
    private IWebDriver? _driver;
    private BloodPressure? _bloodPressure;
    private BPCategory? _calculatedCategory;

    [Given("I have opened the blood pressure calculator")]
    public async Task GivenIHaveOpenedTheBloodPressureCalculator()
    {
        _host = Program.CreateHostBuilder(Array.Empty<string>())
            .ConfigureWebHost(webBuilder => webBuilder
                .UseEnvironment(Environments.Production)
                .UseUrls("http://127.0.0.1:0"))
            .Build();

        await _host.StartAsync();

        var address = _host.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()?
            .Addresses.Single()
            ?? throw new InvalidOperationException("The web host did not publish a listening address.");

        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArgument("--headless=new");
        chromeOptions.AddArgument("--window-size=1280,800");
        if (OperatingSystem.IsLinux())
        {
            chromeOptions.AddArgument("--disable-dev-shm-usage");
        }

        var chromeBinary = Environment.GetEnvironmentVariable("CHROME_BIN");
        if (!string.IsNullOrWhiteSpace(chromeBinary))
        {
            chromeOptions.BinaryLocation = chromeBinary;
        }

        _driver = new ChromeDriver(chromeOptions);
        Driver.Navigate().GoToUrl(address);
        WaitForDisplayed(By.Name("BP.Systolic"));
    }

    [When("I submit a reading with systolic {int} and diastolic {int}")]
    public async Task WhenISubmitAReading(int systolic, int diastolic)
    {
        var systolicInput = WaitForDisplayed(By.Name("BP.Systolic"));
        systolicInput.Clear();
        systolicInput.SendKeys(systolic.ToString(CultureInfo.InvariantCulture));

        var diastolicInput = WaitForDisplayed(By.Name("BP.Diastolic"));
        diastolicInput.Clear();
        diastolicInput.SendKeys(diastolic.ToString(CultureInfo.InvariantCulture));

        Driver.FindElement(By.CssSelector("#form1 input[type='submit'][value='Submit']")).Click();
    }

    [Given("I have a blood pressure reading of {int} over {int}")]
    public void GivenIHaveABloodPressureReading(int systolic, int diastolic)
    {
        _bloodPressure = new BloodPressure
        {
            Systolic = systolic,
            Diastolic = diastolic
        };
    }

    [When("I calculate its category directly")]
    public void WhenICalculateItsCategoryDirectly()
    {
        _calculatedCategory = (_bloodPressure
            ?? throw new InvalidOperationException("No blood pressure reading has been provided."))
            .Category;
    }

    [Then("the category is {string}")]
    public void ThenTheCategoryIs(string expectedCategory)
    {
        var expected = Enum.Parse<BPCategory>(expectedCategory, ignoreCase: true);
        Assert.AreEqual(expected, _calculatedCategory);
    }

    [Then("the calculator displays category {string}")]
    public Task ThenTheCalculatorDisplaysCategory(string expectedCategory)
    {
        var category = WaitForExactText(By.CssSelector("#form1 .form-group"), expectedCategory);
        Assert.IsNotNull(category, $"Expected category to be visible: {expectedCategory}");
        return Task.CompletedTask;
    }

    [Then("the calculator displays validation message {string}")]
    public Task ThenTheCalculatorDisplaysValidationMessage(string expectedMessage)
    {
        var message = WaitForExactText(
            By.CssSelector("#form1 .validation-summary-errors"),
            expectedMessage);
        Assert.IsNotNull(message, $"Expected validation message to be visible: {expectedMessage}");
        return Task.CompletedTask;
    }

    [Then("no blood pressure category is displayed")]
    public Task ThenNoBloodPressureCategoryIsDisplayed()
    {
        foreach (var category in new[]
        {
            "Low Blood Pressure",
            "Ideal Blood Pressure",
            "Pre-High Blood Pressure",
            "High Blood Pressure"
        })
        {
            var displayed = Driver
                .FindElements(By.CssSelector("#form1 .form-group"))
                .Any(element => element.Displayed
                    && string.Equals(element.Text.Trim(), category, StringComparison.Ordinal));
            Assert.IsFalse(displayed, $"Unexpected category displayed: {category}");
        }

        return Task.CompletedTask;
    }

    [AfterScenario]
    public async Task StopCalculator()
    {
        try
        {
            if (_driver is not null)
            {
                try
                {
                    _driver.Quit();
                }
                finally
                {
                    _driver.Dispose();
                }
            }
        }
        finally
        {
            if (_host is not null)
            {
                try
                {
                    await _host.StopAsync();
                }
                finally
                {
                    _host.Dispose();
                }
            }
        }
    }

    private IWebDriver Driver =>
        _driver ?? throw new InvalidOperationException("The calculator browser has not been opened.");

    private WebDriverWait Wait => new(Driver, TimeSpan.FromSeconds(10));

    private IWebElement WaitForDisplayed(By selector) =>
        Wait.Until(driver => driver.FindElements(selector).FirstOrDefault(element => element.Displayed))
        ?? throw new WebDriverTimeoutException($"No visible element was found using {selector}.");

    private IWebElement WaitForExactText(By selector, string expectedText) =>
        Wait.Until(driver => driver.FindElements(selector).FirstOrDefault(element =>
            element.Displayed
            && string.Equals(element.Text.Trim(), expectedText, StringComparison.Ordinal)))
        ?? throw new WebDriverTimeoutException($"Expected visible text was not found: {expectedText}");
}
