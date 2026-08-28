using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        var dirs = Directory.GetDirectories(@"F:\C# projects\HR_system\tests\Buy2.Domain.Tests\StrykerOutput");
        var latest = dirs.OrderByDescending(d => new DirectoryInfo(d).CreationTime).First();
        string html = File.ReadAllText(Path.Combine(latest, "reports", "mutation-report.html"));
        var match = Regex.Match(html, @"window\.mutationTestReport\s*=\s*(\{.*?\});\s*</script>", RegexOptions.Singleline);
        if (match.Success)
        {
            File.WriteAllText(@"F:\C# projects\HR_system\report.json", match.Groups[1].Value);
        }
    }
}
