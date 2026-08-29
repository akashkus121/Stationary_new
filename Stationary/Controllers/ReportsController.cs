using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Stationary.Data;
using Stationary.Models;
using System.Data;

namespace Stationary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/reportsapi")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ReportsController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("daily")]
        public IActionResult GetDailyReport([FromQuery] DateTime? date)
        {
            DateTime selected = (date ?? DateTime.Today).Date;
            var vm = TryBuildReportWithStoredProcedure(selected) ?? BuildReportWithEf(selected);
            return Ok(vm);
        }

        [HttpGet("excel")]
        public IActionResult DownloadExcelReport([FromQuery] DateTime? date)
        {
            DateTime selected = (date ?? DateTime.Today).Date;
            var vm = TryBuildReportWithStoredProcedure(selected) ?? BuildReportWithEf(selected);

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.AddWorksheet("Sales Report");
                int row = 1;
                sheet.Cell(row, 1).Value = $"Sales Report - {selected:yyyy-MM-dd}";
                sheet.Range(row, 1, row, 5).Merge().Style.Font.SetBold().Font.FontSize = 14;
                row += 2;

                sheet.Cell(row, 1).Value = "Order ID";
                sheet.Cell(row, 2).Value = "Date";
                sheet.Cell(row, 3).Value = "User";
                sheet.Cell(row, 4).Value = "Items";
                sheet.Cell(row, 5).Value = "Amount ($)";
                sheet.Range(row, 1, row, 5).Style.Font.SetBold();
                row++;

                foreach (var r in vm.Rows.OrderBy(r => r.OrderDate))
                {
                    sheet.Cell(row, 1).Value = r.OrderId;
                    sheet.Cell(row, 2).Value = r.OrderDate.ToString("yyyy-MM-dd HH:mm");
                    sheet.Cell(row, 3).Value = r.Username;
                    sheet.Cell(row, 4).Value = r.Items;
                    sheet.Cell(row, 5).Value = r.Amount;
                    row++;
                }

                row++;
                sheet.Cell(row, 4).Value = "Total Orders:";
                sheet.Cell(row, 5).Value = vm.TotalOrders;
                row++;
                sheet.Cell(row, 4).Value = "Total Items:";
                sheet.Cell(row, 5).Value = vm.TotalItemsSold;
                row++;
                sheet.Cell(row, 4).Value = "Total Sales:";
                sheet.Cell(row, 5).Value = vm.TotalSalesAmount;

                sheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    var fileName = $"SalesReport_{selected:yyyyMMdd}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }

        [HttpGet("pdf")]
        public IActionResult DownloadPdfReport([FromQuery] DateTime? date)
        {
            DateTime selected = (date ?? DateTime.Today).Date;
            var vm = TryBuildReportWithStoredProcedure(selected) ?? BuildReportWithEf(selected);

            byte[] pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Header().Text($"Sales Report - {selected:yyyy-MM-dd}").SemiBold().FontSize(18);
                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(CellStyle).Text("Order ID").SemiBold();
                            header.Cell().Element(CellStyle).Text("Date").SemiBold();
                            header.Cell().Element(CellStyle).Text("User").SemiBold();
                            header.Cell().Element(CellStyle).Text("Items").SemiBold();
                            header.Cell().Element(CellStyle).Text("Amount").SemiBold();

                            static IContainer CellStyle(IContainer c) => c.PaddingVertical(4).DefaultTextStyle(x => x.FontSize(10));
                        });

                        foreach (var r in vm.Rows)
                        {
                            table.Cell().Element(Cell).Text(r.OrderId.ToString());
                            table.Cell().Element(Cell).Text(r.OrderDate.ToString("yyyy-MM-dd HH:mm"));
                            table.Cell().Element(Cell).Text(r.Username);
                            table.Cell().Element(Cell).Text(r.Items.ToString());
                            table.Cell().Element(Cell).Text(r.Amount.ToString("0.00"));

                            static IContainer Cell(IContainer c) => c.PaddingVertical(3).DefaultTextStyle(x => x.FontSize(10));
                        }
                    });

                    page.Footer().AlignRight().Text($"Totals — Orders: {vm.TotalOrders} | Items: {vm.TotalItemsSold} | Sales: ${vm.TotalSalesAmount:0.00}").FontSize(10);
                });
            }).GeneratePdf();

            var pdfName = $"SalesReport_{selected:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", pdfName);
        }

        private SalesReportViewModel? TryBuildReportWithStoredProcedure(DateTime selected)
        {
            try
            {
                var rows = new List<SalesReportRow>();
                int totalOrders = 0;
                int totalItems = 0;
                decimal totalSales = 0m;

                var connection = _db.Database.GetDbConnection();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.usp_GetDailySalesReport";
                    command.CommandType = CommandType.StoredProcedure;

                    var p = command.CreateParameter();
                    p.ParameterName = "@ReportDate";
                    p.Value = selected.Date;
                    command.Parameters.Add(p);

                    if (connection.State != ConnectionState.Open)
                        connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new SalesReportRow
                            {
                                OrderId = reader.GetInt32(reader.GetOrdinal("OrderId")),
                                OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                                Username = reader.GetString(reader.GetOrdinal("Username")),
                                Items = reader.GetInt32(reader.GetOrdinal("Items")),
                                Amount = reader.GetDecimal(reader.GetOrdinal("Amount"))
                            });
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            totalOrders = reader.GetInt32(reader.GetOrdinal("TotalOrders"));
                            totalItems = reader.GetInt32(reader.GetOrdinal("TotalItemsSold"));
                            totalSales = reader.GetDecimal(reader.GetOrdinal("TotalSalesAmount"));
                        }
                    }
                }

                return new SalesReportViewModel
                {
                    SelectedDate = selected,
                    Rows = rows.OrderByDescending(r => r.OrderDate).ToList(),
                    TotalOrders = totalOrders,
                    TotalItemsSold = totalItems,
                    TotalSalesAmount = totalSales
                };
            }
            catch
            {
                return null;
            }
        }

        private SalesReportViewModel BuildReportWithEf(DateTime selected)
        {
            DateTime start = selected.Date;
            DateTime end = selected.Date.AddDays(1);

            var orders = _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Date >= start && o.Date < end)
                .ToList();

            var userIds = orders.Select(o => o.UserId).Distinct().ToList();
            var users = _db.Users.Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.Username);

            return new SalesReportViewModel
            {
                SelectedDate = selected,
                TotalOrders = orders.Count,
                TotalSalesAmount = orders.Sum(o => o.TotalAmount),
                TotalItemsSold = orders.Sum(o => o.OrderItems != null ? o.OrderItems.Sum(i => i.Quantity) : 0),
                Rows = orders.OrderByDescending(o => o.Date).Select(o => new SalesReportRow
                {
                    OrderId = o.Id,
                    OrderDate = o.Date,
                    Username = users.TryGetValue(o.UserId, out var name) ? name : "User#" + o.UserId,
                    Items = o.OrderItems != null ? o.OrderItems.Sum(i => i.Quantity) : 0,
                    Amount = o.TotalAmount
                }).ToList()
            };
        }
    }
}
