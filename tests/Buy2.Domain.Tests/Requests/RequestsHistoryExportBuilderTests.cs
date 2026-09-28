using Buy2.Application.DTOs.Requests;
using Buy2.Application.Features.Requests.GetRequestsHistory;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Buy2.Domain.Tests.Requests;

public class RequestsHistoryExportBuilderTests
{
    private List<RequestHistorySummaryDto> CreateSampleData()
    {
        return new List<RequestHistorySummaryDto>
        {
            new RequestHistorySummaryDto(
                Id: 101,
                EmployeeId: 1,
                EmployeeName: "John Doe",
                EmployeeCode: "EMP-001",
                RequestType: "Annual Leave",
                Category: "Leave",
                SubmittedAt: new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc),
                StartDate: new DateTime(2026, 9, 10),
                EndDate: new DateTime(2026, 9, 15),
                ManagerName: "Sarah Connor",
                ManagerStatus: "Approved",
                ManagerComment: "Approved, coverage arranged.",
                HrStatus: "Approved",
                HrComment: "Policy confirmed.",
                Status: "Approved",
                RejectionReason: null,
                ResolvedAt: new DateTime(2026, 9, 2, 14, 0, 0, DateTimeKind.Utc),
                AttachmentsCount: 0
            ),
            new RequestHistorySummaryDto(
                Id: 102,
                EmployeeId: 2,
                EmployeeName: "Alice Smith, Jr.",
                EmployeeCode: "EMP-002",
                RequestType: "Equipment Request",
                Category: "Asset",
                SubmittedAt: new DateTime(2026, 9, 3, 11, 0, 0, DateTimeKind.Utc),
                StartDate: null,
                EndDate: null,
                ManagerName: "Sarah Connor",
                ManagerStatus: "Rejected",
                ManagerComment: "Budget exceeded for this quarter.",
                HrStatus: "Pending",
                HrComment: null,
                Status: "Rejected",
                RejectionReason: "Budget Constraints",
                ResolvedAt: new DateTime(2026, 9, 4, 9, 0, 0, DateTimeKind.Utc),
                AttachmentsCount: 1
            )
        };
    }

    [Fact]
    public void BuildCsv_GeneratesProperHeadersAndEscapedValues()
    {
        var records = CreateSampleData();
        var bytes = RequestHistoryExportBuilder.BuildCsv(records);
        var csv = Encoding.UTF8.GetString(bytes);

        Assert.NotEmpty(csv);
        Assert.Contains("Request ID,Employee ID,Employee Name", csv);
        Assert.Contains("101,1,John Doe,EMP-001,Annual Leave", csv);
        // Verify comma escaping in "Alice Smith, Jr."
        Assert.Contains("\"Alice Smith, Jr.\"", csv);
        Assert.Contains("Budget Constraints", csv);
    }

    [Fact]
    public void BuildExcel_GeneratesValidWorkbookWithData()
    {
        var records = CreateSampleData();
        var bytes = RequestHistoryExportBuilder.BuildExcel(records);

        Assert.NotEmpty(bytes);
        using var ms = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(ms);

        var sheet = workbook.Worksheet("Requests History");
        Assert.NotNull(sheet);

        Assert.Equal("Request ID", sheet.Cell(1, 1).GetString());
        Assert.Equal(101, sheet.Cell(2, 1).GetValue<int>());
        Assert.Equal("John Doe", sheet.Cell(2, 3).GetString());
        Assert.Equal("Approved", sheet.Cell(2, 15).GetString());

        Assert.Equal(102, sheet.Cell(3, 1).GetValue<int>());
        Assert.Equal("Alice Smith, Jr.", sheet.Cell(3, 3).GetString());
        Assert.Equal("Rejected", sheet.Cell(3, 15).GetString());
    }
}
