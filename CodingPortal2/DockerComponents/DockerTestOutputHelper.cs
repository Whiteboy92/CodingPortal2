using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
namespace CodingPortal2.DockerComponents;

public static class DockerTestOutputHelper
{
    public static string FormatTestsOutputForUser(string outputString, string totalTests)
    {
        int passedCount = CountOccurrences(outputString, "Passed");
        

        return $"Tests passed: {passedCount} / {totalTests}";
    }

    internal static int GetPassedTestsCount(string outputString)
    {
        return CountOccurrences(outputString, "Passed");
    }

    internal static int GetTotalTestsCount(string outputString)
    {
        if (int.TryParse(outputString, out var totalCount))
        {
            return totalCount;
        }
        Console.WriteLine($"Failed to parse '{outputString}' as an integer.");
        return 0; // Default value or appropriate handling
    }

    
    internal static double GetMemoryUsage(string outputString)
    {
        var memoryUsageRegex = new Regex(@"Memory used: (\d+\.\d+) MB");
        var match = memoryUsageRegex.Match(outputString);

        if (match.Success)
        {
            return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        return 0;
    }
    
    internal static double GetExecutionTime(string outputString)
    {
        var executionTimeRegex = new Regex(@"Execution time: (\d+\.\d+) ms");
        var match = executionTimeRegex.Match(outputString);

        if (match.Success)
        {
            return double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        return 0;
    }

    private static int CountOccurrences(string input, string searchTerm)
    {
        int index = input.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
        int count = 0;

        while (index != -1)
        {
            count++;
            index = input.IndexOf(searchTerm, index + 1, StringComparison.OrdinalIgnoreCase);
        }

        return count;
    }
    
    public static Task<string> RemoveHtmlFormatting(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var textContent = GetTextContent(doc.DocumentNode);

        textContent = Regex.Replace(textContent, "&nbsp;", " ");
        textContent = Regex.Replace(textContent, "&gt;", ">");
        textContent = Regex.Replace(textContent, "&lt;", "<");
        textContent = Regex.Replace(textContent, "&amp;", "&");

        return Task.FromResult(textContent.Trim());
    }



    private static string GetTextContent(HtmlNode node)
    {
        if (node.NodeType == HtmlNodeType.Text)
        {
            return node.InnerHtml;
        }

        var sb = new StringBuilder();
        foreach (var childNode in node.ChildNodes)
        {
            sb.Append(GetTextContent(childNode));
        }

        if (node.Name is "p" or "br")
        {
            sb.AppendLine();
        }

        return sb.ToString();
    }
}