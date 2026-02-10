using Xunit.Abstractions;
using Xunit.Sdk;

namespace MiniDashboard.UITest;

/// <summary>
/// Orders tests alphabetically by method name to ensure sequential execution (T1, T2, T3...).
/// </summary>
public class AlphabeticalOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        return testCases.OrderBy(tc => tc.TestMethod.Method.Name);
    }
}
