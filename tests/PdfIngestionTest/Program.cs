using System.Diagnostics;
using PartsCopilot.Models;
using PartsCopilot.Services;

var pdfPath = args.Length > 0 ? args[0] : Path.Combine("..", "..", "docs", "PART LIST -911-912-1965- 1969.pdf");

if (!File.Exists(pdfPath))
{
    Console.Error.WriteLine($"PDF not found: {Path.GetFullPath(pdfPath)}");
    return 1;
}

Console.WriteLine($"PDF: {Path.GetFullPath(pdfPath)}");
Console.WriteLine(new string('=', 80));

// Phase 1: Extract pages
Console.WriteLine("\n[Phase 1] Extracting pages with PdfPig...");
var sw = Stopwatch.StartNew();
var ingestion = new PdfIngestionService();
var pages = await ingestion.ExtractPagesAsync(pdfPath, "test-manual", CancellationToken.None);
sw.Stop();

Console.WriteLine($"  Pages extracted: {pages.Count}");
Console.WriteLine($"  Duration: {sw.Elapsed.TotalSeconds:F1}s");

// Page classification summary
var byType = pages.GroupBy(p => p.PageType).OrderByDescending(g => g.Count());
Console.WriteLine("\n  Page classification:");
foreach (var g in byType)
    Console.WriteLine($"    {g.Key}: {g.Count()}");

var withIllustration = pages.Count(p => p.Illustration != null);
Console.WriteLine($"  Pages with illustration ID: {withIllustration}");

// Phase 2: Dump raw text from sample part_table pages
var partTablePages = pages.Where(p => p.PageType == "part_table").ToList();
Console.WriteLine($"\n[Phase 2] Raw text from first 3 part_table pages:");
foreach (var page in partTablePages.Take(3))
{
    Console.WriteLine(new string('-', 80));
    Console.WriteLine($"  Page {page.PageNumber} | Illustration: {page.Illustration ?? "N/A"} | Type: {page.PageType}");
    Console.WriteLine(new string('-', 80));
    Console.WriteLine(page.RawText);
    Console.WriteLine();
}

// Also dump a few non-part_table pages for comparison
var otherPages = pages.Where(p => p.PageType != "part_table" && p.PageType != "other").Take(2).ToList();
if (otherPages.Count > 0)
{
    Console.WriteLine($"\n[Phase 2b] Sample non-part_table pages:");
    foreach (var page in otherPages)
    {
        Console.WriteLine(new string('-', 80));
        Console.WriteLine($"  Page {page.PageNumber} | Type: {page.PageType}");
        Console.WriteLine(new string('-', 80));
        Console.WriteLine(page.RawText.Length > 500 ? page.RawText[..500] + "..." : page.RawText);
        Console.WriteLine();
    }
}

// Phase 3: Parse parts
Console.WriteLine("\n[Phase 3] Parsing parts with PorscheClassicManualParser...");
var parser = new PorscheClassicManualParser();
var canParse = parser.CanParse(pages);
Console.WriteLine($"  CanParse: {canParse}");

if (!canParse)
{
    Console.Error.WriteLine("  Parser cannot handle this manual. Dumping first 5 pages text for diagnosis:");
    foreach (var page in pages.Take(5))
    {
        Console.WriteLine($"  --- Page {page.PageNumber} ---");
        Console.WriteLine(page.RawText.Length > 300 ? page.RawText[..300] : page.RawText);
    }
    return 1;
}

sw.Restart();
var progress = new Progress<int>(pct => { if (pct % 20 == 0) Console.Write($"  {pct}%..."); });
var parts = await parser.ParseAsync(pages, "test-manual", progress, CancellationToken.None);
sw.Stop();
Console.WriteLine();

Console.WriteLine($"\n  Parts parsed: {parts.Count}");
Console.WriteLine($"  Duration: {sw.Elapsed.TotalSeconds:F1}s");
Console.WriteLine($"  Part tables processed: {partTablePages.Count}");
Console.WriteLine($"  Avg parts per table page: {(partTablePages.Count > 0 ? (double)parts.Count / partTablePages.Count : 0):F1}");

// Quality diagnostics
var missingDesc = parts.Count(p => string.IsNullOrWhiteSpace(p.Description));
var missingQty = parts.Count(p => string.IsNullOrWhiteSpace(p.Quantity));
var missingModel = parts.Count(p => string.IsNullOrWhiteSpace(p.Model));
var missingPos = parts.Count(p => string.IsNullOrWhiteSpace(p.Position));
var missingIllustration = parts.Count(p => string.IsNullOrWhiteSpace(p.Illustration));

Console.WriteLine($"\n  Quality:");
Console.WriteLine($"    With description: {parts.Count - missingDesc}/{parts.Count}");
Console.WriteLine($"    With quantity: {parts.Count - missingQty}/{parts.Count}");
Console.WriteLine($"    With model: {parts.Count - missingModel}/{parts.Count}");
Console.WriteLine($"    With position: {parts.Count - missingPos}/{parts.Count}");
Console.WriteLine($"    With illustration: {parts.Count - missingIllustration}/{parts.Count}");

// Unique part numbers
var uniqueParts = parts.Select(p => p.PartNumberNormalized).Distinct().Count();
Console.WriteLine($"    Unique part numbers: {uniqueParts}");

// Part number pattern breakdown
var partNumberPatterns = parts
    .GroupBy(p =>
    {
        if (p.PartNumber.StartsWith("PCG")) return "PCG xxx xxx xx";
        if (p.PartNumber.StartsWith("N ")) return "N xxx xxx xx";
        if (System.Text.RegularExpressions.Regex.IsMatch(p.PartNumber, @"^\d{3}\s\d{3}\s\d{3}\s\d{2}$")) return "NNN NNN NNN NN";
        return "Other: " + p.PartNumber;
    })
    .OrderByDescending(g => g.Count());

Console.WriteLine($"\n  Part number patterns:");
foreach (var g in partNumberPatterns.Take(10))
    Console.WriteLine($"    {g.Key}: {g.Count()}");

// Phase 4: Sample output
Console.WriteLine($"\n[Phase 4] Sample parsed parts (first 20):");
Console.WriteLine($"  {"Pos",-5} {"Part Number",-20} {"Description",-35} {"Qty",-5} {"Model",-10} {"Remark",-10} {"Ill",-8} {"Pg",-4}");
Console.WriteLine($"  {new string('-', 97)}");

foreach (var part in parts.Take(20))
{
    var desc = part.Description.Length > 33 ? part.Description[..33] + ".." : part.Description;
    Console.WriteLine($"  {part.Position ?? "",-5} {part.PartNumber,-20} {desc,-35} {part.Quantity ?? "",-5} {part.Model ?? "",-10} {part.Remark ?? "",-10} {part.Illustration ?? "",-8} {part.PageNumber,-4}");
}

// Also show some parts from the middle and end
if (parts.Count > 40)
{
    var mid = parts.Count / 2;
    Console.WriteLine($"\n  ... Parts from middle (index {mid}):");
    foreach (var part in parts.Skip(mid).Take(10))
    {
        var desc = part.Description.Length > 33 ? part.Description[..33] + ".." : part.Description;
        Console.WriteLine($"  {part.Position ?? "",-5} {part.PartNumber,-20} {desc,-35} {part.Quantity ?? "",-5} {part.Model ?? "",-10} {part.Remark ?? "",-10} {part.Illustration ?? "",-8} {part.PageNumber,-4}");
    }
}

if (parts.Count > 20)
{
    Console.WriteLine($"\n  ... Last 10 parts:");
    foreach (var part in parts.TakeLast(10))
    {
        var desc = part.Description.Length > 33 ? part.Description[..33] + ".." : part.Description;
        Console.WriteLine($"  {part.Position ?? "",-5} {part.PartNumber,-20} {desc,-35} {part.Quantity ?? "",-5} {part.Model ?? "",-10} {part.Remark ?? "",-10} {part.Illustration ?? "",-8} {part.PageNumber,-4}");
    }
}

Console.WriteLine($"\n{'=',-80}");
Console.WriteLine("Done.");
return 0;
