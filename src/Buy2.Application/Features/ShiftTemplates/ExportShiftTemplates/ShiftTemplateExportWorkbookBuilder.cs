using Buy2.Application.Features.ShiftTemplates.DTOs;
using ClosedXML.Excel;

namespace Buy2.Application.Features.ShiftTemplates.ExportShiftTemplates;

public static class ShiftTemplateExportWorkbookBuilder
{
    public static byte[] Build(IReadOnlyList<ShiftTemplateDetailsDto> templates)
    {
        using var workbook = new XLWorkbook();
        FillTemplatesSheet(workbook.Worksheets.Add("Templates"), templates);
        FillBlocksSheet(workbook.Worksheets.Add("Blocks"), templates);
        FillSitesSheet(workbook.Worksheets.Add("Sites"), templates);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void FillTemplatesSheet(IXLWorksheet sheet, IReadOnlyList<ShiftTemplateDetailsDto> templates)
    {
        WriteHeader(sheet, ["Id", "Name", "StartTime", "EndTime", "CreationDate", "LastUpdated", "AssignedSitesCount", "Sites"]);

        var row = 2;
        foreach (var template in templates)
        {
            var sites = template.Sites.Count == 0
                ? string.Empty
                : string.Join("; ", template.Sites.Select(s => s.SiteName));

            sheet.Cell(row, 1).Value = template.Id;
            sheet.Cell(row, 2).Value = template.Name;
            sheet.Cell(row, 3).Value = template.StartTime;
            sheet.Cell(row, 4).Value = template.EndTime;
            sheet.Cell(row, 5).Value = template.CreationDate;
            sheet.Cell(row, 6).Value = template.LastUpdated;
            sheet.Cell(row, 7).Value = template.NumberOfAssignedSites;
            sheet.Cell(row, 8).Value = sites;
            row++;
        }

        FinalizeSheet(sheet);
    }

    private static void FillBlocksSheet(IXLWorksheet sheet, IReadOnlyList<ShiftTemplateDetailsDto> templates)
    {
        WriteHeader(sheet, ["TemplateId", "TemplateName", "BlockId", "StartTime", "EndTime", "JobRoleTitle", "AssignedUserName"]);

        var row = 2;
        foreach (var template in templates)
        {
            foreach (var block in template.ShiftBlocks)
            {
                sheet.Cell(row, 1).Value = template.Id;
                sheet.Cell(row, 2).Value = template.Name;
                sheet.Cell(row, 3).Value = block.Id;
                sheet.Cell(row, 4).Value = block.StartTime;
                sheet.Cell(row, 5).Value = block.EndTime;
                sheet.Cell(row, 6).Value = block.JobRoleTitle;
                sheet.Cell(row, 7).Value = block.AssignedUserName;
                row++;
            }
        }

        FinalizeSheet(sheet);
    }

    private static void FillSitesSheet(IXLWorksheet sheet, IReadOnlyList<ShiftTemplateDetailsDto> templates)
    {
        WriteHeader(sheet, ["TemplateId", "TemplateName", "SiteId", "SiteName"]);

        var row = 2;
        foreach (var template in templates)
        {
            foreach (var site in template.Sites)
            {
                sheet.Cell(row, 1).Value = template.Id;
                sheet.Cell(row, 2).Value = template.Name;
                sheet.Cell(row, 3).Value = site.SiteId;
                sheet.Cell(row, 4).Value = site.SiteName;
                row++;
            }
        }

        FinalizeSheet(sheet);
    }

    private static void WriteHeader(IXLWorksheet sheet, string[] columns)
    {
        for (var col = 0; col < columns.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = columns[col];
        }

        var headerRow = sheet.Row(1);
        headerRow.Style.Font.Bold = true;
    }

    private static void FinalizeSheet(IXLWorksheet sheet)
    {
        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();
    }
}
