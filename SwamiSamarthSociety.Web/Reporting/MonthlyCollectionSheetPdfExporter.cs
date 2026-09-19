using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SwamiSamarthSociety.Data.Entities;
using SwamiSamarthSociety.Services;

namespace SwamiSamarthSociety.Web.Reporting
{
    // PDF twin of MonthlyCollectionSheetExcelExporter -- same title block, Marathi headers,
    // orange rows for active loans, yellow "एकूण रक्कम" column, and a totals row, laid out as a
    // printable A4-landscape table instead of a workbook.
    public static class MonthlyCollectionSheetPdfExporter
    {
        private const string FontFamily = "Nirmala UI";

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

        private static readonly string HeaderBg = "#BDD7EE";
        private static readonly string TitleBg = "#C6E0B4";
        private static readonly string LoanRowBg = "#FCE4D6";
        private static readonly string TotalAmountBg = "#FFFF00";
        private static readonly string BorderColor = "#B7BEC9";

        public static byte[] Export(MonthlyCycle cycle, List<MonthlyCycleRow> rows) => BuildDocument(cycle, rows).GeneratePdf();

        public static IDocument BuildDocument(MonthlyCycle cycle, List<MonthlyCycleRow> rows)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontFamily(FontFamily).FontSize(8));

                    page.Header().Background(TitleBg).Padding(5).Column(col =>
                    {
                        col.Item().AlignCenter().Text("|| श्री स्वामी समर्थ सोसायटी ||").Bold().FontSize(14);
                        col.Item().AlignCenter().Text("चिंबळी ता. खेड, जि. पुणे - ४१२ १०५").FontSize(9);
                        col.Item().PaddingTop(2).AlignCenter().Text($"महिना - {MarathiMonths[cycle.Month]} {cycle.Year}").Bold().FontSize(10);
                    });

                    page.Content().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(28);
                            columns.RelativeColumn(2.4f);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn(1.3f);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var h in Headers)
                            {
                                header.Cell().Background(HeaderBg).Border(0.5f).BorderColor(BorderColor)
                                    .Padding(2).AlignCenter().Text(h).Bold().FontSize(7);
                            }
                        });

                        decimal totalShare = 0, totalCumShare = 0, totalOpening = 0, totalPrincipal = 0,
                            totalInterest = 0, totalPenalty = 0, totalArrears = 0, totalAmount = 0, totalRemaining = 0;

                        var srNo = 1;
                        foreach (var row in rows)
                        {
                            var bg = row.HasActiveLoan ? LoanRowBg : "#FFFFFF";

                            table.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(2).AlignCenter().Text(srNo.ToString());
                            table.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(2).Text(row.FullNameMarathi ?? row.FullName);
                            table.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(2).AlignRight().Text($"{row.ShareExpected:N0}");
                            table.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(2).AlignRight().Text($"{row.TotalShare:N0}");
                            table.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(2).AlignRight()
                                .Text(row.HasActiveLoan ? $"{row.OpeningPrincipal:N0}" : "");
                            table.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(2).AlignRight()
                                .Text(row.HasActiveLoan ? $"{row.PrincipalDue:N0}" : "");
                            table.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(2).AlignRight()
                                .Text(row.HasActiveLoan ? $"{row.InterestDue:N0}" : "");
                            table.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(2).AlignRight()
                                .Text(row.HasActiveLoan && row.PenaltyDue > 0 ? $"{row.PenaltyDue:N0}" : "");
                            table.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(2).AlignRight()
                                .Text(row.HasActiveLoan && row.ArrearsDue > 0 ? $"{row.ArrearsDue:N0}" : "");
                            table.Cell().Background(TotalAmountBg).Border(0.5f).BorderColor(BorderColor).Padding(2).AlignRight().Text($"{row.TotalAmount:N0}");
                            table.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(2).AlignRight()
                                .Text(row.HasActiveLoan ? $"{row.RemainingBalance:N0}" : "");

                            totalShare += row.ShareExpected;
                            totalCumShare += row.TotalShare;
                            totalOpening += row.OpeningPrincipal ?? 0;
                            totalPrincipal += row.PrincipalDue ?? 0;
                            totalInterest += row.InterestDue ?? 0;
                            totalPenalty += row.PenaltyDue ?? 0;
                            totalArrears += row.ArrearsDue ?? 0;
                            totalAmount += row.TotalAmount;
                            totalRemaining += row.RemainingBalance ?? 0;
                            srNo++;
                        }

                        void TotalCell(string text) =>
                            table.Cell().Background(TitleBg).Border(0.5f).BorderColor(BorderColor)
                                .Padding(2).AlignRight().Text(text).Bold().FontSize(8);

                        table.Cell().Background(TitleBg).Border(0.5f).BorderColor(BorderColor).Padding(2).Text("");
                        table.Cell().Background(TitleBg).Border(0.5f).BorderColor(BorderColor).Padding(2).Text("Total").Bold();
                        TotalCell($"{totalShare:N0}");
                        TotalCell($"{totalCumShare:N0}");
                        TotalCell($"{totalOpening:N0}");
                        TotalCell($"{totalPrincipal:N0}");
                        TotalCell($"{totalInterest:N0}");
                        TotalCell($"{totalPenalty:N0}");
                        TotalCell($"{totalArrears:N0}");
                        TotalCell($"{totalAmount:N0}");
                        TotalCell($"{totalRemaining:N0}");
                    });

                    page.Footer().AlignRight().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });
        }
    }
}
