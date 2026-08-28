$html = Get-Content 'tests\Buy2.Domain.Tests\StrykerOutput\2026-08-28.05-31-26\reports\mutation-report.html' -Raw
if ($html -match '(?s)window\.mutationTestReport\s*=\s*(\{.*?\});\s*</script>') {
    Set-Content 'report.json' $matches[1]
}
