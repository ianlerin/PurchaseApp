using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WorkerRecord;

namespace PurchaseBlazorApp2.Service
{
    public class HRPDFHelper
    {
        public byte[] GenerateWagePdf(WageRecord info)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // ====== PAGE CONTENT =======
                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        // ===== NEW HEADER =====
                        col.Item().Text($"Wages {info.Year}/{info.Month:D2} (LCDA MSB Pineapple SDN BHD)")
                             .FontSize(16).Bold().AlignCenter();
                        col.Item()
                           .PaddingBottom(10) // Apply padding at container level
                           .Text("GENERAL WORKER (PLANTING + DIPPING)")
                           .FontSize(12)
                           .Bold()
                           .AlignCenter();

                        // ===== TABLE =====
                        BuildWageTable(col, info.WageRecords);

                        // ===== SIGNATURE SECTION =====
                        col.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Prepared By").Bold();
                                c.Item().PaddingTop(30).Text("____________________________");
                                c.Item().PaddingTop(5).Text("Name:");
                                c.Item().Text("Date:");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("Approved By").Bold();
                                c.Item().PaddingTop(30).Text("____________________________");
                                c.Item().PaddingTop(5).Text("Name:");
                                c.Item().Text("Date:");
                            });
                        });
                    });

                });
            });

            return document.GeneratePdf();
        }

        // ========= WAGE TABLE CONSTRUCTION =========
        private void BuildWageTable(ColumnDescriptor column, List<SingleWageRecord> wageRecords)
        {
            column.Item().Table(table =>
            {
                // -------- COLUMNS --------
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(2); // Worker Name
                    for (int i = 0; i < 5; i++) // Daily, OT, Sunday, Monthly, Hourly
                    {
                        cols.RelativeColumn(); // Hours
                        cols.RelativeColumn(); // Rate
                        cols.RelativeColumn(); // Wages
                    }
                    cols.RelativeColumn(); // Total Wages
                });

                // -------- HEADER --------
                table.Header(header =>
                {
                    string[] headers =
                    {
                    "Worker Name",
                    "Daily Hours", "Daily Rate", "Daily Wages",
                    "OT Hours", "OT Rate", "OT Wages",
                    "Sunday Hours", "Sunday Rate", "Sunday Wages",
                    "Monthly Hours", "Monthly Rate", "Monthly Wages",
                    "Hourly Hours", "Hourly Rate", "Hourly Wages",
                    "Total Wages"
                };

                    foreach (var h in headers)
                    {
                        header.Cell().Element(CellHeaderStyle).AlignCenter().Text(h).Bold();
                    }
                });

                // -------- BODY ROWS --------
                decimal totalDaily = 0, totalOT = 0, totalSunday = 0, totalMonthly = 0, totalHourly = 0, grandTotal = 0;

                foreach (var r in wageRecords)
                {
                    table.Cell().Element(CellBody).Text(r.Name);

                    // Daily
                    table.Cell().Element(CellBody).AlignCenter().Text(r.DailyHours.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignCenter().Text(r.DailyRate.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignRight().Text(r.Daily_wages.ToString("0.##"));
                    totalDaily += r.Daily_wages;

                    // OT
                    table.Cell().Element(CellBody).AlignCenter().Text(r.OTHours.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignCenter().Text(r.OTRate.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignRight().Text(r.OT_wages.ToString("0.##"));
                    totalOT += r.OT_wages;

                    // Sunday
                    table.Cell().Element(CellBody).AlignCenter().Text(r.SundayHours.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignCenter().Text(r.SundayRate.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignRight().Text(r.Sunday_wages.ToString("0.##"));
                    totalSunday += r.Sunday_wages;

                    // Monthly
                    table.Cell().Element(CellBody).AlignCenter().Text(r.MonthlyHours.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignCenter().Text(r.MonthlyRate.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignRight().Text(r.Monthly_wages.ToString("0.##"));
                    totalMonthly += r.Monthly_wages;

                    // Hourly
                    table.Cell().Element(CellBody).AlignCenter().Text(r.HourlyHours.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignCenter().Text(r.HourlyRate.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignRight().Text(r.Hourly_wages.ToString("0.##"));
                    totalHourly += r.Hourly_wages;

                    // Total
                    table.Cell().Element(CellBody).AlignRight().Text(r.Total_wages.ToString("0.##"));
                    grandTotal += r.Total_wages;
                }

                // -------- TOTAL ROW --------
                table.Cell().Element(CellHeaderStyle).Text("TOTAL").AlignCenter().Bold();

                // Empty cells for hours/rates
                for (int i = 0; i < 5; i++)
                {
                    table.Cell().Element(CellHeaderStyle).Text(""); // Hours
                    table.Cell().Element(CellHeaderStyle).Text(""); // Rate
                    table.Cell().Element(CellHeaderStyle).AlignRight().Text(
                        i == 0 ? totalDaily.ToString("0.##") :
                        i == 1 ? totalOT.ToString("0.##") :
                        i == 2 ? totalSunday.ToString("0.##") :
                        i == 3 ? totalMonthly.ToString("0.##") :
                                  totalHourly.ToString("0.##")
                    );
                }

                // Grand total
                table.Cell().Element(CellHeaderStyle).AlignRight().Text(grandTotal.ToString("0.##")).Bold();

                // -------- CELL STYLES --------
                static IContainer CellHeaderStyle(IContainer container) =>
                    container.Background("#EEEEEE").Border(1).Padding(5);

                static IContainer CellBody(IContainer container) =>
                    container.Border(1).Padding(5);
            });
        }
    }
}
