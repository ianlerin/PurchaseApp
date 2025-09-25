using PurchaseBlazorApp2.Components.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PurchaseBlazorApp2
{
    public class POPDFHelper
    {
        public byte[] GeneratePurchaseOrderPdf(PurchaseOrderRecord PO, List<RequestItemInfo> RequestItemInfos)
        {
          
            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // HEADER
                    page.Header().Column(header =>
                    {
                        header.Item().Row(row =>
                        {
                            var logoFolder = Path.Combine(AppContext.BaseDirectory, "Resource");
                            var logoPath = Path.Combine(logoFolder, "PineappleLogo.jpeg");
                            row.RelativeItem(1)
                               .Height(60)
                               .Image(logoPath, ImageScaling.FitHeight);

                            row.RelativeItem(3).Column(col =>
                            {
                                col.Item().Text(PO.mycompanyname).Bold();
                                col.Item().Text(PO.myaddress);
                                col.Item().Text($"Email: {PO.myemail}");
                                col.Item().Text($"Tel: {PO.tel}");
                            });
                        });

                        header.Item().PaddingTop(10).AlignCenter().Text("Purchase Order").FontSize(16).Bold();
                    });

                    // CONTENT
                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        // Vendor, Ship To, Order Info
                        column.Item().Row(row =>
                        {
                            // Vendor Info
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Vendor").Bold();
                                col.Item().Text(PO.suppliercompanyname ?? "");
                                col.Item().Text($"Contact Person: {PO.suppliercontactperson}");
                                col.Item().Text($"Contact: {PO.suppliercontact}");
                                col.Item().Text($"Email: {PO.supplieremail}");
                            });

                            // Ship To Info
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Ship To:").Bold();
                                col.Item().Text(PO.shiptocompanyname ?? "");
                                col.Item().Text(PO.warehouseaddress ?? "");
                                col.Item().Text($"Attn: {PO.receivingperson}");
                                col.Item().Text($"Contact: {PO.shippingcontact}");
                            });

                            // Order Info
                            row.ConstantItem(120).Column(col =>
                            {
                                col.Item().Text("Order #").Bold();
                                col.Item().Text(PO.PO_ID ?? "");
                                col.Item().Text("Order Date:").Bold();
                                col.Item().Text(PO.orderdate.ToString("dd/MM/yyyy"));
                            });
                        });

                        // ITEMS TABLE
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30); // No
                                columns.RelativeColumn(3); // Description
                                columns.ConstantColumn(60); // Qty
                                columns.ConstantColumn(60); // Unit Price
                                columns.ConstantColumn(70); // Total
                            });

                            // Table Header
                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("No.").Bold();
                                header.Cell().Element(CellStyle).Text("Description of Goods/ Services").Bold();
                                header.Cell().Element(CellStyle).AlignCenter().Text("Quantity").Bold();
                                header.Cell().Element(CellStyle).AlignCenter().Text("Unit Price").Bold();
                                header.Cell().Element(CellStyle).AlignCenter().Text("Total Price (RM)").Bold();

                                static IContainer CellStyle(IContainer container) => container.Border(1).Padding(5);
                            });

                            // Table Rows
                            int index = 1;
                            foreach (var item in RequestItemInfos)
                            {
                                table.Cell().Element(CellStyle).Text(index++.ToString());
                                table.Cell().Element(CellStyle).Text(item.RequestItem);
                                table.Cell().Element(CellStyle).AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text(item.UnitPrice.ToString("F2"));
                                table.Cell().Element(CellStyle).AlignRight().Text(item.TotalPrice.ToString("F2"));

                                static IContainer CellStyle(IContainer container) => container.Border(1).Padding(5);
                            }

                            // Totals
                            table.Cell().ColumnSpan(4).AlignRight().Padding(5).Text("Subtotal:");
                            table.Cell().AlignRight().Padding(5).Text(PO.SubTotal.ToString("F2"));

                            table.Cell().ColumnSpan(4).AlignRight().Padding(5).Text("Tax:");
                            table.Cell().AlignRight().Padding(5).Text(PO.Tax.ToString("F2"));

                            table.Cell().ColumnSpan(4).AlignRight().Padding(5).Text("Total:");
                            table.Cell().AlignRight().Padding(5).Text(PO.GetTotal().ToString("F2"));
                        });

                        // Terms & Conditions
                        column.Item().PaddingTop(10).Border(1).Padding(5).Column(tc =>
                        {
                            tc.Item().Text("Terms & Conditions").Bold();
                            tc.Item().Text($"Delivery Date: On or before {PO.DeliveryDate:dd MMMM yyyy}");
                            tc.Item().Text($"Delivery Method: {PO.DeliveryMethod}");
                            tc.Item().Text($"Payment Terms: {PO.PaymentMethod}");
                        });

                        // Remarks
                        column.Item().Text($"Remarks: {PO.remark}")
                              .Italic();

                        // Authorization
                        column.Item().PaddingTop(20).Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Authorized By").Bold();
                                col.Item().Text("Name: Authorized Signatory");
                                col.Item().Text("Designation: Procurement Manager");
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Signature").Bold();
                                col.Item().PaddingTop(20).Text("__________________"); 
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Date").Bold();
                                col.Item().PaddingTop(20).Text("__________________"); 
                            });
                        });
                    });

                    // Footer
                    page.Footer().AlignCenter().Text("Generated with QuestPDF");
                });
            });

            return doc.GeneratePdf();
        }
    }
}
