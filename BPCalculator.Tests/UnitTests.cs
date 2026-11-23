using System.Diagnostics;
using BPCalculator;
using BPCalculator.Pages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPCalculator.Tests;

[TestClass]
[TestCategory("Unit")]
public sealed class BloodPressureCategoryTests
{
    [DataTestMethod]
    [DataRow(89, 59, BPCategory.Low)]
    [DataRow(90, 50, BPCategory.Ideal)]
    [DataRow(80, 60, BPCategory.Ideal)]
    [DataRow(119, 79, BPCategory.Ideal)]
    [DataRow(90, 59, BPCategory.Ideal)]
    [DataRow(89, 60, BPCategory.Ideal)]
    [DataRow(120, 70, BPCategory.PreHigh)]
    [DataRow(120, 79, BPCategory.PreHigh)]
    [DataRow(110, 80, BPCategory.PreHigh)]
    [DataRow(119, 80, BPCategory.PreHigh)]
    [DataRow(139, 89, BPCategory.PreHigh)]
    [DataRow(140, 70, BPCategory.High)]
    [DataRow(110, 90, BPCategory.High)]
    [DataRow(139, 90, BPCategory.High)]
    public void Category_ReturnsExpectedCategory(
        int systolic,
        int diastolic,
        BPCategory expectedCategory)
    {
        var bloodPressure = new BloodPressure
        {
            Systolic = systolic,
            Diastolic = diastolic
        };

        Assert.AreEqual(expectedCategory, bloodPressure.Category);
    }
}

[TestClass]
[TestCategory("Unit")]
public sealed class BloodPressurePageModelTests
{
    [TestMethod]
    public void OnGet_InitializesDefaultReading()
    {
        var page = new BloodPressureModel();

        page.OnGet();

        Assert.AreEqual(100, page.BP.Systolic);
        Assert.AreEqual(60, page.BP.Diastolic);
    }

    [TestMethod]
    public void OnPost_WhenSystolicIsGreaterThanDiastolic_DoesNotAddError()
    {
        var page = CreatePage(120, 80);

        var result = page.OnPost();

        Assert.IsInstanceOfType(result, typeof(PageResult));
        Assert.IsTrue(page.ModelState.IsValid);
    }

    [DataTestMethod]
    [DataRow(80, 80)]
    [DataRow(79, 80)]
    public void OnPost_WhenSystolicIsNotGreaterThanDiastolic_AddsModelError(
        int systolic,
        int diastolic)
    {
        var page = CreatePage(systolic, diastolic);

        var result = page.OnPost();

        Assert.IsInstanceOfType(result, typeof(PageResult));
        Assert.AreEqual(
            "Systolic must be greater than Diastolic",
            page.ModelState[string.Empty]?.Errors.Single().ErrorMessage);
    }

    private static BloodPressureModel CreatePage(int systolic, int diastolic)
    {
        return new BloodPressureModel
        {
            BP = new BloodPressure { Systolic = systolic, Diastolic = diastolic },
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };
    }
}

[TestClass]
[TestCategory("Unit")]
public sealed class ErrorPageModelTests
{
    [TestMethod]
    public void ShowRequestId_IsFalseForEmptyIdAndTrueForNonEmptyId()
    {
        var page = CreatePage();

        Assert.IsFalse(page.ShowRequestId);

        page.RequestId = "request-id";

        Assert.IsTrue(page.ShowRequestId);
    }

    [TestMethod]
    public void OnGet_UsesHttpContextTraceIdentifierWhenThereIsNoCurrentActivity()
    {
        var previousActivity = Activity.Current;
        Activity.Current = null;

        try
        {
            var page = CreatePage();
            page.HttpContext.TraceIdentifier = "trace-id";

            page.OnGet();

            Assert.AreEqual("trace-id", page.RequestId);
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    [TestMethod]
    public void OnGet_PrefersCurrentActivityId()
    {
        var previousActivity = Activity.Current;
        using var activity = new Activity("test-operation").Start();

        try
        {
            var page = CreatePage();

            page.OnGet();

            Assert.AreEqual(activity.Id, page.RequestId);
        }
        finally
        {
            Activity.Current = previousActivity;
        }
    }

    private static ErrorModel CreatePage()
    {
        return new ErrorModel(NullLogger<ErrorModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext() }
        };
    }
}

[TestClass]
[TestCategory("Unit")]
public sealed class StartupTests
{
    [TestMethod]
    public void Configuration_ReturnsConfigurationPassedToConstructor()
    {
        var configuration = new ConfigurationBuilder().Build();
        var startup = new Startup(configuration);

        Assert.AreSame(configuration, startup.Configuration);
    }
}
