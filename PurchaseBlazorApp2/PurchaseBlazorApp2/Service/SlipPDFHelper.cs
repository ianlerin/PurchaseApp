using PurchaseBlazorApp2.Client.Pages.HR;
using PurchaseBlazorApp2.Components.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WorkerRecord;

namespace PurchaseBlazorApp2
{
    public class SlipPDFHelper
    {
        public byte[] GeneratePaymentSlip(SingleWageRecord r )
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(20);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content()
                        .Border(1)
                        .Column(col =>
                        {
                            // Header
                            col.Item().Padding(5).Row(row =>
                            {
                                row.RelativeItem().Text("LCDA MSB PINEAPPLE SDN.BHD.").Bold().FontSize(14);
                                row.RelativeItem().AlignRight().Text("END PAYMENT – JANUARY 2019").Bold();
                                col.Item().LineHorizontal(1).LineColor(Colors.Black);
                            });
                      

                            // Employee details
                            col.Item().Padding(5).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Spacing(5);
                                    c.Item().Text($"EMPLOYEE NO: {r.ID}");
                                    c.Item().Text($"NAME: {r.Name}");
                                    c.Item().Text($"IC/PASSPORT: {r.Passport}");
                                    c.Item().Text($"DEPARTMENT: {r.Designation}");
                                    c.Item().Text($"BASE RATE: {r.DailyRate:C}");
                                    c.Item().Text($"WORKING DAYS: {0}");
                                });

                                row.RelativeItem().Column(c =>
                                {
                                    c.Spacing(5);
                                    c.Item().Text($"POSITION: {0}");
                                    c.Item().Text($"EPF NO: {r.EPFCategory}");
                                    c.Item().Text($"SOCSO NO: {r.SocsoCategory}");
                                    c.Item().Text($"TAX NO: {0}");
                                });
                            });
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().LineHorizontal(1).LineColor(Colors.Black);
                            });


                            // Income 
                            col.Item().Padding(5).Row(row =>
                            {
                                // Income 
                                row.RelativeItem().Column(c =>
                                {
                                    c.Spacing(5);
                                    c.Item().Text("EARNINGS / INCOME").Bold();
                                    c.Item().LineHorizontal(1).LineColor(Colors.Black);
                                    c.Item().Text($"BASIC PAY: {r.BasicPay:C}");
                                    c.Item().Text($"ALLOWANCE: {r.Allowance:C}");
                                });

                           
                                row.ConstantItem(1).Background(Colors.Black);

                                // Deduction
                                row.RelativeItem().Column(c =>
                                {
                                    c.Spacing(5);
                                    c.Item().Text("DEDUCTION").Bold();
                                    c.Item().LineHorizontal(1).LineColor(Colors.Black);
                                    c.Item().Text($"EMPLOYEE EPF: {r.EPF_Employee}");
                                    c.Item().Text($"EMPLOYEE SOCSO: {r.Socso_Employee}");
                                    c.Item().Text($"DEDUCTION: {r.Deduction}");
                                    c.Item().Text($"DEDUCTION REASON: {r.Deduction_Reason}");
                                });
                            });

                            
                            col.Item().LineHorizontal(1).LineColor(Colors.Black);
                                          
                            //Totals
                            col.Item().Padding(5).Row(row =>
                            {
                                // Gross Pay 
                                row.RelativeItem().Column(c => 
                                {   c.Spacing(5);
                                    c.Item().Text($"GROSS PAY: {r.Gross_wages:C}").Bold();
                                    c.Item().LineHorizontal(1).LineColor(Colors.Black);
                                });
                                row.ConstantItem(1).Background(Colors.Black);

                                // Total Deduction 
                                row.RelativeItem().Column(c =>
                                {
                                    c.Spacing(5);
                                    c.Item().Text($"TOTAL DEDUCTION: {r.EPF_Employee + r.Socso_Employee + r.Deduction}").Bold();
                                    c.Item().LineHorizontal(1).LineColor(Colors.Black);

                                   
                                    c.Item().Text($"NET PAY: {r.Total_wages:C}").Bold();
                                    c.Item().LineHorizontal(1).LineColor(Colors.Black);
                                });
                            });


                            // Table
                            col.Item().Padding(5).Row(row =>
                            {
                                row.ConstantItem(200).PaddingLeft(20).PaddingTop(10).Column(c =>
                                {
                                    c.Item()
                                     .Border(1)     
                                     .Padding(3)
                                     .Table(table =>
                                     {
                                         table.ColumnsDefinition(columns =>
                                         {
                                             columns.RelativeColumn();
                                             columns.RelativeColumn();
                                         });

                                         table.Cell().BorderRight(1).Padding(3).Text("EPF");  
                                         table.Cell().Padding(3).AlignRight().Text($"{r.EPF_Employer}");
                              
                                         table.Cell().BorderRight(1).Padding(3).Text("SOCSO");
                                         table.Cell().Padding(3).AlignRight().Text($"{r.Socso_Employer}");

                                         table.Cell().BorderRight(1).Padding(3).Text("EIS");
                                         table.Cell().Padding(3).AlignRight().Text($"{0}");
                                     });
                                });

                                row.RelativeItem();
                            });

                            // Sign
                            col.Item().PaddingTop(50).Padding(5).Row(row =>
                            {   
                                row.RelativeItem().Text("APPROVED BY:_____________________");
                                row.RelativeItem().Text("RECEIVED BY:_____________________");
                            });
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}
