using Buy2.Application.DTOs.Requests;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Buy2.Application.Features.Requests.GetRequestsHistory;

public static class RequestHistoryExportBuilder
{
    public static byte[] BuildCsv(IReadOnlyList<RequestHistorySummaryDto> records)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Request ID,Employee ID,Employee Name,Employee Code,Request Type,Category,Submitted At,Start Date,End Date,Manager Name,Manager Status,Manager Comment,HR Status,HR Comment,Overall Status,Rejection Reason,Resolved At,Attachments Count");

        foreach (var r in records)
        {
            sb.Append(r.Id).Append(",");
            sb.Append(r.EmployeeId).Append(",");
            sb.Append(EscapeCsv(r.EmployeeName)).Append(",");
            sb.Append(EscapeCsv(r.EmployeeCode ?? string.Empty)).Append(",");
            sb.Append(EscapeCsv(r.RequestType)).Append(",");
            sb.Append(EscapeCsv(r.Category)).Append(",");
            sb.Append(r.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss")).Append(",");
            sb.Append(r.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty).Append(",");
            sb.Append(r.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty).Append(",");
            sb.Append(EscapeCsv(r.ManagerName ?? string.Empty)).Append(",");
            sb.Append(EscapeCsv(r.ManagerStatus)).Append(",");
            sb.Append(EscapeCsv(r.ManagerComment ?? string.Empty)).Append(",");
            sb.Append(EscapeCsv(r.HrStatus)).Append(",");
            sb.Append(EscapeCsv(r.HrComment ?? string.Empty)).Append(",");
            sb.Append(EscapeCsv(r.Status)).Append(",");
            sb.Append(EscapeCsv(r.RejectionReason ?? string.Empty)).Append(",");
            sb.Append(r.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty).Append(",");
            sb.Append(r.AttachmentsCount);
            sb.AppendLine();
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public static byte[] BuildExcel(IReadOnlyList<RequestHistorySummaryDto> records)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Requests History");

        var headers = new[]
        {
            "Request ID", "Employee ID", "Employee Name", "Employee Code", "Request Type", "Category",
            "Submitted At", "Start Date", "End Date", "Manager Name", "Manager Status", "Manager Comment",
            "HR Status", "HR Comment", "Overall Status", "Rejection Reason", "Resolved At", "Attachments Count"
        };

        for (int col = 0; col < headers.Length; col++)
        {
            var cell = worksheet.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
        }

        for (int row = 0; row < records.Count; row++)
        {
            var r = records[row];
            int rIdx = row + 2;

            worksheet.Cell(rIdx, 1).Value = r.Id;
            worksheet.Cell(rIdx, 2).Value = r.EmployeeId;
            worksheet.Cell(rIdx, 3).Value = r.EmployeeName;
            worksheet.Cell(rIdx, 4).Value = r.EmployeeCode ?? string.Empty;
            worksheet.Cell(rIdx, 5).Value = r.RequestType;
            worksheet.Cell(rIdx, 6).Value = r.Category;
            worksheet.Cell(rIdx, 7).Value = r.SubmittedAt.ToString("yyyy-MM-dd HH:mm:ss");
            worksheet.Cell(rIdx, 8).Value = r.StartDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            worksheet.Cell(rIdx, 9).Value = r.EndDate?.ToString("yyyy-MM-dd") ?? string.Empty;
            worksheet.Cell(rIdx, 10).Value = r.ManagerName ?? string.Empty;
            worksheet.Cell(rIdx, 11).Value = r.ManagerStatus;
            worksheet.Cell(rIdx, 12).Value = r.ManagerComment ?? string.Empty;
            worksheet.Cell(rIdx, 13).Value = r.HrStatus;
            worksheet.Cell(rIdx, 14).Value = r.HrComment ?? string.Empty;
            worksheet.Cell(rIdx, 15).Value = r.Status;
            worksheet.Cell(rIdx, 16).Value = r.RejectionReason ?? string.Empty;
            worksheet.Cell(rIdx, 17).Value = r.ResolvedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
            worksheet.Cell(rIdx, 18).Value = r.AttachmentsCount;
        }

        worksheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\r") || value.Contains("\n"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
