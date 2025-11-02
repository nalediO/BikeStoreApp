using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Web.Mvc;
using BikeStoreApp.Models;
using System.Data.Entity;
using Rotativa;

namespace BikeStoreApp.Controllers
{
    public class ReportsController : Controller
    {
        private BikeStoresEntities db = new BikeStoresEntities();

        public ReportsController()
        {
            db.Database.CommandTimeout = 180;
        }

        public async Task<ActionResult> Index(string reportType = "sales")
        {
            ViewBag.Title = "Reports";
            ViewBag.SelectedReportType = reportType;

            await LoadReportData(reportType);

            await LoadArchiveData();

            return View();
        }
        private async Task LoadReportData(string reportType)
        {
            switch (reportType?.ToLower())
            {
                case "sales":
                    await LoadSalesReport();
                    break;
                case "popular":
                    await LoadPopularProducts();
                    break;
                case "customer":
                    await LoadCustomerPerformance();
                    break;
                case "stock":
                    await LoadStockReport();
                    break;
                case "frequency":
                    await LoadSalesFrequency();
                    break;
                case "staff":
                    await LoadStaffPerformance();
                    break;
                case "store":
                    await LoadStorePerformance();
                    break;
                default:
                    await LoadSalesReport();
                    break;
            }
        }
        private async Task LoadSalesReport()
        {
            var report = await (from oi in db.order_items
                                join o in db.orders on oi.order_id equals o.order_id
                                join c in db.customers on o.customer_id equals c.customer_id
                                join s in db.staffs on o.staff_id equals s.staff_id
                                join p in db.products on oi.product_id equals p.product_id
                                join b in db.brands on p.brand_id equals b.brand_id
                                join cat in db.categories on p.category_id equals cat.category_id
                                orderby o.order_date descending
                                select new
                                {
                                    Customer = c.first_name + " " + c.last_name,
                                    Staff = s.first_name + " " + s.last_name,
                                    Product = p.product_name,
                                    Brand = b.brand_name,
                                    Category = cat.category_name,
                                    Quantity = oi.quantity,
                                    Total = oi.quantity * oi.list_price * (1 - oi.discount)
                                }).Take(100).ToListAsync();

            var staffPerformance = report
                .GroupBy(r => r.Staff)
                .Select(g => new
                {
                    Staff = g.Key,
                    TotalSales = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.TotalSales)
                .Take(10)
                .ToList();

            ViewBag.ReportData = report;
            ViewBag.ChartData = staffPerformance;
            ViewBag.ReportTitle = "Current Sales Report";
            ViewBag.ChartType = "bar";
        }
        private async Task LoadPopularProducts()
        {
            var report = await (from oi in db.order_items
                                join p in db.products on oi.product_id equals p.product_id
                                join b in db.brands on p.brand_id equals b.brand_id
                                join cat in db.categories on p.category_id equals cat.category_id
                                group oi by new { p.product_name, b.brand_name, cat.category_name } into g
                                orderby g.Sum(x => x.quantity) descending
                                select new
                                {
                                    Product = g.Key.product_name,
                                    Brand = g.Key.brand_name,
                                    Category = g.Key.category_name,
                                    Quantity = g.Sum(x => x.quantity),
                                    Revenue = g.Sum(x => x.quantity * x.list_price * (1 - x.discount))
                                }).Take(10).ToListAsync();

            ViewBag.ReportData = report;
            ViewBag.ChartData = report;
            ViewBag.ReportTitle = "Popular Products Report";
            ViewBag.ChartType = "pie";
        }
        private async Task LoadCustomerPerformance()
        {
            var report = await (from o in db.orders
                                join c in db.customers on o.customer_id equals c.customer_id
                                join oi in db.order_items on o.order_id equals oi.order_id
                                group new { o, oi } by new { c.customer_id, c.first_name, c.last_name, c.city, c.state } into g
                                orderby g.Sum(x => x.oi.quantity * x.oi.list_price * (1 - x.oi.discount)) descending
                                select new
                                {
                                    Customer = g.Key.first_name + " " + g.Key.last_name,
                                    Location = g.Key.city + ", " + g.Key.state,
                                    TotalOrders = g.Select(x => x.o.order_id).Distinct().Count(),
                                    TotalProducts = g.Sum(x => x.oi.quantity),
                                    TotalSpent = g.Sum(x => x.oi.list_price * x.oi.quantity * (1 - x.oi.discount))
                                }).Take(15).ToListAsync();

            ViewBag.ReportData = report;
            ViewBag.ChartData = report;
            ViewBag.ReportTitle = "Customer Performance Ranking";
            ViewBag.ChartType = "bar";
        }
        private async Task LoadStockReport()
        {
            var report = await (from st in db.stocks
                                join p in db.products on st.product_id equals p.product_id
                                join b in db.brands on p.brand_id equals b.brand_id
                                join cat in db.categories on p.category_id equals cat.category_id
                                join store in db.stores on st.store_id equals store.store_id
                                where st.quantity > 0
                                orderby st.quantity descending
                                select new
                                {
                                    Product = p.product_name,
                                    Brand = b.brand_name,
                                    Category = cat.category_name,
                                    Store = store.store_name,
                                    StockQuantity = st.quantity,
                                    ListPrice = p.list_price,
                                    StockValue = st.quantity * p.list_price
                                }).Take(100).ToListAsync();

            var stockByStore = report
                .GroupBy(r => r.Store)
                .Select(g => new
                {
                    Store = g.Key,
                    TotalValue = g.Sum(x => x.StockValue)
                })
                .OrderByDescending(x => x.TotalValue)
                .ToList();

            ViewBag.ReportData = report;
            ViewBag.ChartData = stockByStore;
            ViewBag.ReportTitle = "Stock Items Report";
            ViewBag.ChartType = "bar";
        }

        private async Task LoadSalesFrequency()
        {
            var report = await (from o in db.orders
                                join oi in db.order_items on o.order_id equals oi.order_id
                                group oi by new
                                {
                                    Year = o.order_date.Year,
                                    Month = o.order_date.Month
                                } into g
                                orderby g.Key.Year, g.Key.Month
                                select new
                                {
                                    Period = g.Key.Year + "-" + g.Key.Month.ToString("D2"),
                                    TotalOrders = g.Select(x => x.order_id).Distinct().Count(),
                                    ProductsSold = g.Sum(x => x.quantity),
                                    Revenue = g.Sum(x => x.quantity * x.list_price * (1 - x.discount))
                                }).ToListAsync();

            ViewBag.ReportData = report;
            ViewBag.ChartData = report;
            ViewBag.ReportTitle = "Sales Frequency Report";
            ViewBag.ChartType = "line";
        }

        private async Task LoadStaffPerformance()
        {
            var report = await (from o in db.orders
                                join s in db.staffs on o.staff_id equals s.staff_id
                                join oi in db.order_items on o.order_id equals oi.order_id
                                join store in db.stores on s.store_id equals store.store_id
                                group new { o, oi } by new { s.staff_id, s.first_name, s.last_name, store.store_name } into g
                                orderby g.Sum(x => x.oi.quantity * x.oi.list_price * (1 - x.oi.discount)) descending
                                select new
                                {
                                    Staff = g.Key.first_name + " " + g.Key.last_name,
                                    Store = g.Key.store_name,
                                    TotalOrders = g.Select(x => x.o.order_id).Distinct().Count(),
                                    ProductsSold = g.Sum(x => x.oi.quantity),
                                    TotalRevenue = g.Sum(x => x.oi.quantity * x.oi.list_price * (1 - x.oi.discount))
                                }).Take(10).ToListAsync();

            ViewBag.ReportData = report;
            ViewBag.ChartData = report;
            ViewBag.ReportTitle = "Staff Performance Ranking";
            ViewBag.ChartType = "bar";
        }


        private async Task LoadStorePerformance()
        {
            var report = await (from o in db.orders
                                join store in db.stores on o.store_id equals store.store_id
                                join oi in db.order_items on o.order_id equals oi.order_id
                                group new { o, oi } by new { store.store_id, store.store_name, store.city, store.state } into g
                                orderby g.Sum(x => x.oi.quantity * x.oi.list_price * (1 - x.oi.discount)) descending
                                select new
                                {
                                    Store = g.Key.store_name,
                                    Location = g.Key.city + ", " + g.Key.state,
                                    TotalOrders = g.Select(x => x.o.order_id).Distinct().Count(),
                                    ProductsSold = g.Sum(x => x.oi.quantity),
                                    TotalRevenue = g.Sum(x => x.oi.quantity * x.oi.list_price * (1 - x.oi.discount))
                                }).ToListAsync();

            ViewBag.ReportData = report;
            ViewBag.ChartData = report;
            ViewBag.ReportTitle = "Store Performance Ranking";
            ViewBag.ChartType = "bar";
        }

        private async Task LoadArchiveData()
        {
            var files = await Task.Run(() =>
            {
                string path = Server.MapPath("~/App_Data/SavedReports/");
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return Directory.GetFiles(path)
                    .Select(f => new ReportFileViewModel
                    {
                        Name = Path.GetFileName(f),
                        Date = System.IO.File.GetCreationTime(f).ToString("yyyy-MM-dd HH:mm"),
                        Size = (new FileInfo(f).Length / 1024.0).ToString("F2") + " KB"
                    })
                    .OrderByDescending(x => x.Date)
                    .ToList();
            });

            ViewBag.ArchiveFiles = files;
        }

        [HttpPost]
        [ValidateInput(false)]
        public async Task<ActionResult> SaveReport(string reportHtml, string filename, string fileType, string currentReport)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reportHtml))
                {
                    TempData["Error"] = "No report data to save";
                    return RedirectToAction("Index", new { reportType = currentReport });
                }

                if (string.IsNullOrWhiteSpace(filename))
                {
                    filename = "Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                }

                // Sanitize filename
                char[] invalidChars = Path.GetInvalidFileNameChars();
                foreach (char c in invalidChars)
                {
                    filename = filename.Replace(c.ToString(), "_");
                }
                filename = filename.Replace(" ", "_");

                string directoryPath = Server.MapPath("~/App_Data/SavedReports/");
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string fullPath = Path.Combine(directoryPath, filename + "." + fileType);

                await Task.Run(() =>
                {
                    if (fileType == "html")
                    {
                        string completeHtml = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>BikeStores Report</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 20px; }}
        th {{ background-color: #0066cc; color: white; padding: 10px; text-align: left; }}
        td {{ padding: 8px; border: 1px solid #ddd; }}
        tr:nth-child(even) {{ background-color: #f2f2f2; }}
        h2 {{ color: #0066cc; text-align: center; }}
        .report-info {{ background: #f5f5f5; padding: 10px; margin: 20px 0; border-radius: 5px; }}
    </style>
</head>
<body>
    <h2> BikeStores Report</h2>
    <div class='report-info'>
        <strong>Generated:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}<br/>
        <strong>Filename:</strong> {filename}.html
    </div>
    {reportHtml}
</body>
</html>";
                        System.IO.File.WriteAllText(fullPath, completeHtml, System.Text.Encoding.UTF8);
                    }
                    else if (fileType == "csv")
                    {
                        string csvContent = ConvertHtmlTableToCsv(reportHtml);
                        System.IO.File.WriteAllText(fullPath, csvContent, System.Text.Encoding.UTF8);
                    }
                });

                TempData["Success"] = $" Report saved as {filename}.{fileType}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $" Error saving report: {ex.Message}";
            }

            return RedirectToAction("Index", new { reportType = currentReport });
        }
        public async Task<ActionResult> DeleteFile(string name, string currentReport = "sales")
        {
            try
            {
                await Task.Run(() =>
                {
                    string path = Server.MapPath("~/App_Data/SavedReports/" + name);
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                });

                TempData["Success"] = $" File {name} deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $" Error deleting file: {ex.Message}";
            }

            return RedirectToAction("Index", new { reportType = currentReport });
        }

        public FileResult Download(string name)
        {
            string path = Server.MapPath("~/App_Data/SavedReports/" + name);
            string contentType = MimeMapping.GetMimeMapping(path);
            return File(path, contentType, name);
        }

        public async Task<ActionResult> ExportToPDF(string type)
        {
            db.Database.CommandTimeout = 180;

            try
            {
                await LoadReportData(type);
                ViewBag.ReportTitle = ViewBag.ReportTitle ?? "Report";

                return new ViewAsPdf("PdfTemplate", ViewBag.ReportData)
                {
                    FileName = $"{type}_Report_{DateTime.Now:yyyyMMdd}.pdf",
                    PageSize = Rotativa.Options.Size.A4,
                    PageOrientation = Rotativa.Options.Orientation.Landscape
                };
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error generating PDF: " + ex.Message;
                return RedirectToAction("Index");
            }
        }
        private string ConvertHtmlTableToCsv(string html)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(html))
                    return "";

                html = System.Text.RegularExpressions.Regex.Replace(html, @"<th[^>]*>", "");
                html = System.Text.RegularExpressions.Regex.Replace(html, @"</th>", ",");
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<td[^>]*>", "");
                html = System.Text.RegularExpressions.Regex.Replace(html, @"</td>", ",");
                html = System.Text.RegularExpressions.Regex.Replace(html, @"</tr>", "\r\n");
                html = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]*>", "");
                html = System.Web.HttpUtility.HtmlDecode(html);

                var lines = html.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                var cleanedLines = lines
                    .Select(line => line.Trim().TrimEnd(','))
                    .Where(line => !string.IsNullOrWhiteSpace(line));

                return string.Join("\r\n", cleanedLines);
            }
            catch
            {
                return html;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}