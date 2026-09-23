using ClosedXML.Excel;
using SwamiSamarthSociety.Data.Entities;
using SwamiSamarthSociety.Services;

namespace SwamiSamarthSociety.Web.Reporting
{
    // Renders a monthly cycle's collection sheet as an .xlsx, matching the layout the society
    // used to keep by hand: title block, Marathi headers, orange rows for active loans, a yellow
    // "एकूण रक्कम" column, and a totals row.
    public static class MonthlyCollectionSheetExcelExporter
    {
        private static readonly string[] MarathiMonths =
        {
            "", "जानेवारी", "फेब्रुवारी", "मार्च", "एप्रिल", "मे", "जून",
            "जुलै", "ऑगस्ट", "सप्टेंबर", "ऑक्टोबर", "नोव्हेंबर", "डिसेंबर"
        };

        private static readonly string[] Headers =
        {
            "अ. क्र", "सभासदाचे नाव", "शेअर रक्कम", "एकूण शेअर", "मागील कर्ज बाकी रक्कम",
            "एकूण मुद्दल", "एकूण व्याज", "दंड", "थकबाकी", "एकूण रक्कम", "शिल्लक कर्ज"
        };

        public static byte[] Export(Society society, MonthlyCycle cycle, List<MonthlyCycleRow> rows)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Collection Sheet");
            ws.Style.Font.FontName = "Nirmala UI";
            ws.RightToLeft = false;

            const int lastCol = 11;

            var titleLine = $"|| {society.NameMarathi ?? society.Name} ||";
            if (!string.IsNullOrWhiteSpace(society.Address)) titleLine += $"\n{society.Address}";
            ws.Range(1, 1, 2, 7).Merge().Value = titleLine;
            ws.Range(1, 1, 2, 7).Style.Font.SetBold().Font.SetFontSize(14);
            ws.Range(1, 1, 2, 7).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#C6E0B4"));
            ws.Range(1, 1, 2, 7).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Range(1, 1, 2, 7).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            ws.Range(1, 1, 2, 7).Style.Alignment.SetWrapText(true);

            ws.Range(1, 8, 2, lastCol).Merge().Value = $"महिना - {MarathiMonths[cycle.Month]} {cycle.Year}";
            ws.Range(1, 8, 2, lastCol).Style.Font.SetBold();
            ws.Range(1, 8, 2, lastCol).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#C6E0B4"));
            ws.Range(1, 8, 2, lastCol).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            ws.Range(1, 8, 2, lastCol).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);

            const int headerRow = 3;
            for (var c = 0; c < Headers.Length; c++)
            {
                var cell = ws.Cell(headerRow, c + 1);
                cell.Value = Headers[c];
                cell.Style.Font.SetBold();
                cell.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#BDD7EE"));
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                cell.Style.Alignment.SetWrapText(true);
            }

            var loanRowFill = XLColor.FromHtml("#FCE4D6");
            var totalAmountFill = XLColor.FromHtml("#FFFF00");

            var r = headerRow + 1;
            var srNo = 1;
            foreach (var row in rows)
            {
                ws.Cell(r, 1).Value = srNo++;
                ws.Cell(r, 2).Value = string.IsNullOrWhiteSpace(row.FullNameMarathi) ? row.FullName : row.FullNameMarathi;
                ws.Cell(r, 3).Value = row.ShareExpected;
                ws.Cell(r, 4).Value = row.TotalShare;

                if (row.HasActiveLoan)
                {
                    ws.Cell(r, 5).Value = row.OpeningPrincipal ?? 0;
                    ws.Cell(r, 6).Value = row.PrincipalDue ?? 0;
                    ws.Cell(r, 7).Value = row.InterestDue ?? 0;
                    ws.Cell(r, 8).Value = row.PenaltyDue ?? 0;
                    ws.Cell(r, 9).Value = row.ArrearsDue ?? 0;
                    ws.Cell(r, 11).Value = row.RemainingBalance ?? 0;
                    ws.Range(r, 1, r, lastCol).Style.Fill.SetBackgroundColor(loanRowFill);
                }

                ws.Cell(r, 10).Value = row.TotalAmount;
                ws.Cell(r, 10).Style.Fill.SetBackgroundColor(totalAmountFill);

                ws.Range(r, 3, r, lastCol).Style.NumberFormat.SetFormat("#,##0");
                r++;
            }

            var totalRow = r;
            ws.Cell(totalRow, 2).Value = "Total";
            ws.Cell(totalRow, 3).FormulaA1 = $"SUM(C{headerRow + 1}:C{totalRow - 1})";
            ws.Cell(totalRow, 4).FormulaA1 = $"SUM(D{headerRow + 1}:D{totalRow - 1})";
            ws.Cell(totalRow, 5).FormulaA1 = $"SUM(E{headerRow + 1}:E{totalRow - 1})";
            ws.Cell(totalRow, 6).FormulaA1 = $"SUM(F{headerRow + 1}:F{totalRow - 1})";
            ws.Cell(totalRow, 7).FormulaA1 = $"SUM(G{headerRow + 1}:G{totalRow - 1})";
            ws.Cell(totalRow, 8).FormulaA1 = $"SUM(H{headerRow + 1}:H{totalRow - 1})";
            ws.Cell(totalRow, 9).FormulaA1 = $"SUM(I{headerRow + 1}:I{totalRow - 1})";
            ws.Cell(totalRow, 10).FormulaA1 = $"SUM(J{headerRow + 1}:J{totalRow - 1})";
            ws.Cell(totalRow, 11).FormulaA1 = $"SUM(K{headerRow + 1}:K{totalRow - 1})";
            ws.Range(totalRow, 1, totalRow, lastCol).Style.Font.SetBold();
            ws.Range(totalRow, 1, totalRow, lastCol).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#C6E0B4"));
            ws.Range(totalRow, 3, totalRow, lastCol).Style.NumberFormat.SetFormat("#,##0");

            ws.Range(1, 1, totalRow, lastCol).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Range(headerRow, 1, totalRow, lastCol).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
            ws.Column(2).Width = 24;
            for (var c = 1; c <= lastCol; c++)
            {
                if (c != 2) ws.Column(c).AdjustToContents(headerRow, totalRow);
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
