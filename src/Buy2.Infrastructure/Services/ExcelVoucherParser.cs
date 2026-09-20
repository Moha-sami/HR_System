using Buy2.Application.Common.Interfaces;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace Buy2.Infrastructure.Services;

/// Service responsible for parsing bulk Excel files (.xlsx / .xls) 
//الادمن بيدخل على الاكسل فايل وبيعمل ابديت للفيتشر بتاع الفاوتشر كودز
// ذى نظام ادارة الكوبونات فى المواقع الكبيرة اللى بتبيع منتجات وبتعمل خصومات على المنتجات
// ExcelDataReader is a lightweight and fast library for reading Excel files in .NET applications.
// دى ال library اللى بتستخدمها فى الكود ده عشان تقرا الاكسل فايلز
/// containing digital voucher codes for the Rewards & Gamification Module.
public class ExcelVoucherParser : IExcelVoucherParserService
{
    static ExcelVoucherParser()
    {
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    // Reads an Excel file stream and extracts all non-empty voucher codes from the spreadsheet.
    // <param name="stream">The uploaded Excel file stream from the HTTP request.</param>
    // <returns>A list of clean voucher code strings ready for database insertion.</returns>
    public async Task<List<string>> UploadExcelFileAsync(
        IFormFile file,
        CancellationToken cancellation)
    {
        var extension = Path
            .GetExtension(file.FileName)
            .ToLowerInvariant();

        return extension switch
        {
            ".xlsx" or ".xls" =>
                await ParseExcelAsync(file, cancellation),

            ".csv" =>
                await ParseCsvAsync(file, cancellation),

            _ => throw new InvalidOperationException(
                "Invalid file format.")
        };
    }

    private static async Task<List<string>> ParseExcelAsync(
        IFormFile file,
        CancellationToken cancellation)
    {
        var voucherCodes = new List<string>();

        await using var stream = file.OpenReadStream();

        using var reader = ExcelReaderFactory.CreateReader(stream);

        var voucherCodeColumnIndex = 0;
        var firstRow = true;

        while (reader.Read())
        {
            cancellation.ThrowIfCancellationRequested();

            if (firstRow)
            {
                firstRow = false;

                voucherCodeColumnIndex =
                    FindVoucherCodeColumn(reader);

                // If the first row is a header,
                // don't add it as a voucher code.
                if (HasVoucherCodeHeader(reader))
                {
                    continue;
                }
            }

            var cellValue = reader
                .GetValue(voucherCodeColumnIndex)?
                .ToString()?
                .Trim();

            if (string.IsNullOrWhiteSpace(cellValue))
                continue;

            voucherCodes.Add(cellValue);
        }

        return voucherCodes;
    }

    private static async Task<List<string>> ParseCsvAsync(
        IFormFile file,
        CancellationToken cancellation)
    {
        var voucherCodes = new List<string>();

        using var reader = new StreamReader(
            file.OpenReadStream(),
            Encoding.UTF8);

        var firstLine = true;
        var voucherCodeColumnIndex = 0;

        while (!reader.EndOfStream)
        {
            cancellation.ThrowIfCancellationRequested();

            var line = await reader.ReadLineAsync(cancellation);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line
                .Split(',')
                .Select(x => x.Trim())
                .ToArray();

            if (firstLine)
            {
                firstLine = false;

                var headerIndex = Array.FindIndex(
                    columns,
                    x => x.Equals(
                        "VoucherCode",
                        StringComparison.OrdinalIgnoreCase));

                if (headerIndex >= 0)
                {
                    voucherCodeColumnIndex = headerIndex;
                    continue;
                }

                // No VoucherCode header,
                // so column 0 is used.
                voucherCodeColumnIndex = 0;
            }

            if (voucherCodeColumnIndex >= columns.Length)
                continue;

            var value = columns[voucherCodeColumnIndex];

            if (string.IsNullOrWhiteSpace(value))
                continue;

            voucherCodes.Add(value);
        }

        return voucherCodes;
    }

    private static int FindVoucherCodeColumn(
        IExcelDataReader reader)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var value = reader
                .GetValue(i)?
                .ToString()?
                .Trim();

            if (value?.Equals(
                    "VoucherCode",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                return i;
            }
        }

        return 0;
    }

    private static bool HasVoucherCodeHeader(
        IExcelDataReader reader)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            var value = reader
                .GetValue(i)?
                .ToString()?
                .Trim();

            if (value?.Equals(
                    "VoucherCode",
                    StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }

        return false;
    }
}
