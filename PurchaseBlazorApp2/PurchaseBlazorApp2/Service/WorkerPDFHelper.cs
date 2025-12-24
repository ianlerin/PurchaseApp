using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WorkerRecord;
using System;
using System.Collections.Generic;


namespace PurchaseBlazorApp2.Service
{
    public class WorkerPDFHelper
    {
        public byte[] GenerateWorkerPdf(List<WorkerRecord.WorkerRecord> workers, EWorkerStatus filterStatus)

        {
            var filteredWorkers = workers.FindAll(w => filterStatus == EWorkerStatus.All || w.WorkerStatus == filterStatus);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        // ===== HEADER =====
                        col.Item()
                           .Text($"Worker List - {filterStatus}")
                           .FontSize(16)
                           .Bold()
                           .AlignCenter();

                        col.Item()
                           .PaddingBottom(10)
                           .Text($"Generated on {DateTime.Now:dd/MM/yyyy}")
                           .FontSize(12)
                           .AlignCenter();

                        // ===== TABLE =====
                        BuildWorkerTable(col, filteredWorkers);
                    });
                });
            });

            return document.GeneratePdf();
        }

        private void BuildWorkerTable(ColumnDescriptor column, List<WorkerRecord.WorkerRecord> workers)
        {
            column.Item().Table(table =>
            {
                // -------- COLUMNS --------
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn();      // ID
                    cols.RelativeColumn(2);     // Name
                    cols.RelativeColumn();      //Age
                    cols.RelativeColumn();     //Passport
                    cols.RelativeColumn();     // Status
                    cols.RelativeColumn();     // EPF
                    cols.RelativeColumn();     // Nationality
                    cols.RelativeColumn();     // Monthly
                    cols.RelativeColumn();     // Hourly
                    cols.RelativeColumn();     // Daily
                    cols.RelativeColumn();     // OT
                    cols.RelativeColumn();     // Sunday
                });

                // -------- HEADER --------
                table.Header(header =>
                {
                    string[] headers =
                    {
                        "ID", "Name", "Age","Passport","Status", "EPF", "Nationality",
                        "Monthly", "Hourly", "Daily", "OT", "Sunday"
                    };

                    foreach (var h in headers)
                    {
                        header.Cell().Element(CellHeaderStyle).AlignCenter().Text(h).Bold();
                    }
                });

                // -------- BODY ROWS --------
                foreach (var w in workers)
                {
                    table.Cell().Element(CellBody).Text(w.ID);
                    table.Cell().Element(CellBody).Text(w.Name);
                    table.Cell().Element(CellBody).Text(w.Age);
                    table.Cell().Element(CellBody).Text(w.Passport);
                    table.Cell().Element(CellBody).AlignCenter().Text(w.WorkerStatus.ToString());
                    table.Cell().Element(CellBody).AlignCenter().Text(w.EPFStatus.ToString());
                    table.Cell().Element(CellBody).AlignCenter().Text(w.NationalityStatus.ToString());

                    table.Cell().Element(CellBody).AlignRight().Text(w.MonthlyRate.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignRight().Text(w.HourlyRate.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignRight().Text(w.DailyRate.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignRight().Text(w.OTRate.ToString("0.##"));
                    table.Cell().Element(CellBody).AlignRight().Text(w.SundayRate.ToString("0.##"));
                }

                // -------- CELL STYLES --------
                static IContainer CellHeaderStyle(IContainer container) =>
                    container.Background("#EEEEEE").Border(1).Padding(5);

                static IContainer CellBody(IContainer container) =>
                    container.Border(1).Padding(5);
            });
        }
    }
}
