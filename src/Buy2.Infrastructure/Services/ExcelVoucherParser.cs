using System.Text;
using ExcelDataReader;

namespace Buy2.Infrastructure.Services;

/// Service responsible for parsing bulk Excel files (.xlsx / .xls) 
//الادمن بيدخل على الاكسل فايل وبيعمل ابديت للفيتشر بتاع الفاوتشر كودز
// ذى نظام ادارة الكوبونات فى المواقع الكبيرة اللى بتبيع منتجات وبتعمل خصومات على المنتجات
// ExcelDataReader is a lightweight and fast library for reading Excel files in .NET applications.
// دى ال library اللى بتستخدمها فى الكود ده عشان تقرا الاكسل فايلز
/// containing digital voucher codes for the Rewards & Gamification Module.
public class ExcelVoucherParser
{
    static ExcelVoucherParser()
    {
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    // Reads an Excel file stream and extracts all non-empty voucher codes from the spreadsheet.
    // <param name="stream">The uploaded Excel file stream from the HTTP request.</param>
    // <returns>A list of clean voucher code strings ready for database insertion.</returns>
    public List<string> ParseExcelCodes(Stream stream)
    {
        var voucherCodes = new List<string>();

        if (stream == null || stream.Length == 0)
        {
            return voucherCodes;
        }

        // 1. Create Excel reader from stream (supports both .xlsx and .xls formats)
        using var reader = ExcelReaderFactory.CreateReader(stream);

        // 2. Read through every row in the spreadsheet
        while (reader.Read())
        {
            // Extract text value from column 1 (index 0)
            var cellValue = reader.GetValue(0)?.ToString()?.Trim();

            // 3. Skip header labels and empty cells
            if (!string.IsNullOrWhiteSpace(cellValue) && 
                !cellValue.Equals("VoucherCode", StringComparison.OrdinalIgnoreCase) &&
                !cellValue.Equals("Code", StringComparison.OrdinalIgnoreCase))
            {
                voucherCodes.Add(cellValue);
            }
        }

        return voucherCodes;
    }
}
