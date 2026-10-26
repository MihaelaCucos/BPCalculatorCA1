using BPCalculator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BPCalculator.Tests;

[TestClass]
public sealed class BloodPressureCategoryTests
{
    [DataTestMethod]
    [DataRow(89, 59, BPCategory.Low)]
    [DataRow(90, 50, BPCategory.Ideal)]
    [DataRow(80, 60, BPCategory.Ideal)]
    [DataRow(119, 79, BPCategory.Ideal)]
    [DataRow(120, 70, BPCategory.PreHigh)]
    [DataRow(110, 80, BPCategory.PreHigh)]
    [DataRow(139, 89, BPCategory.PreHigh)]
    [DataRow(140, 70, BPCategory.High)]
    [DataRow(110, 90, BPCategory.High)]
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
