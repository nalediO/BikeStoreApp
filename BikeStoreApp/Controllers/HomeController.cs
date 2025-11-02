using BikeStoreApp.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Mvc;

public class HomeController : Controller
{
    private BikeStoresEntities db = new BikeStoresEntities();

    private const int PageSize = 4;

    public HomeController()
    {
        db.Database.CommandTimeout = 180; 
    }


    public async Task<ActionResult> Index(string brand, string category,
        int staffPage = 1, int customerPage = 1, int productPage = 1)
    {
        var staffQuery = db.staffs.Include(s => s.store).OrderBy(s => s.staff_id);
        int staffTotal = await staffQuery.CountAsync();
        var staffPageList = await staffQuery
            .Skip((staffPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var customerQuery = db.customers.OrderBy(c => c.customer_id);
        int customerTotal = await customerQuery.CountAsync();
        var customerPageList = await customerQuery
            .Skip((customerPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var productsQuery = db.products
            .Include(p => p.brand)
            .Include(p => p.category)
            .OrderBy(p => p.product_id)
            .AsQueryable();

        if (!string.IsNullOrEmpty(brand))
            productsQuery = productsQuery.Where(p => p.brand.brand_name == brand);
        if (!string.IsNullOrEmpty(category))
            productsQuery = productsQuery.Where(p => p.category.category_name == category);

        int productTotal = await productsQuery.CountAsync();
        var productPageList = await productsQuery
            .Skip((productPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var productImages = new Dictionary<int, string>();
        string imagesFolder = HostingEnvironment.MapPath("~/Bikes_Images/");
        if (imagesFolder == null)
            imagesFolder = HostingEnvironment.MapPath("~/Bikes-Images/");

        foreach (var p in productPageList)
        {
            string brandClean = (p.brand?.brand_name ?? "Unknown").Replace(" ", "_");
            string found = null;

            var candidates = new[]
            {
                $"{brandClean}_bike.jpeg", $"{brandClean}_bike.jpg",
                $"{brandClean}1.jpeg", $"{brandClean}1.jpg",
                $"{brandClean}.jpeg", $"{brandClean}.jpg",
                $"{brandClean}_balance_bike1.jpeg",
                $"{brandClean}_Marlin_bike.jpeg",
                $"{brandClean}_Cruiser.jpeg"
            };

            if (!string.IsNullOrEmpty(imagesFolder))
            {
                foreach (var c in candidates)
                {
                    var full = System.IO.Path.Combine(imagesFolder, c);
                    if (System.IO.File.Exists(full))
                    {
                        found = Url.Content("~/Bikes_Images/" + c);
                        break;
                    }
                }
            }

            if (found == null)
                found = Url.Content("~/Bikes_Images/default_bike.jpeg");

            productImages[p.product_id] = found;
        }

        ViewBag.Staffs = staffPageList;
        ViewBag.StaffTotal = staffTotal;
        ViewBag.StaffPage = staffPage;

        ViewBag.Customers = customerPageList;
        ViewBag.CustomerTotal = customerTotal;
        ViewBag.CustomerPage = customerPage;

        ViewBag.Products = productPageList;
        ViewBag.ProductTotal = productTotal;
        ViewBag.ProductPage = productPage;

        ViewBag.PageSize = PageSize;
        ViewBag.Brands = await db.brands.OrderBy(b => b.brand_name).ToListAsync();
        ViewBag.Categories = await db.categories.OrderBy(c => c.category_name).ToListAsync();
        ViewBag.Stores = await db.stores.OrderBy(s => s.store_name).ToListAsync();

        ViewBag.ProductImages = productImages;
        ViewBag.SelectedBrand = brand;
        ViewBag.SelectedCategory = category;

        return View();
    }

    [HttpPost]
    public async Task<ActionResult> CreateStaff(staff newStaff)
    {
        try
        {
            if (ModelState.IsValid)
            {
                newStaff.active = 1; 
                db.staffs.Add(newStaff);
                await db.SaveChangesAsync();
                TempData["Success"] = "Staff member created successfully!";
            }
            else
            {
                TempData["Error"] = "Please fill all required fields.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error creating staff: " + ex.Message;
        }
        return RedirectToAction("Index");
    }



    [HttpPost]
    public async Task<ActionResult> CreateCustomer(customer newCustomer)
    {
        try
        {
            if (ModelState.IsValid)
            {
                db.customers.Add(newCustomer);
                await db.SaveChangesAsync();
                TempData["Success"] = "Customer created successfully!";
            }
            else
            {
                TempData["Error"] = "Please fill all required fields.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error creating customer: " + ex.Message;
        }
        return RedirectToAction("Index");
    }


    public async Task<ActionResult> Maintain()
    {
      
        ViewBag.Staffs = await db.staffs
            .Include(s => s.store)
            .Include(s => s.staff1) 
            .OrderBy(s => s.staff_id)
            .ToListAsync();

        ViewBag.Customers = await db.customers
            .OrderBy(c => c.customer_id)
            .ToListAsync();

        ViewBag.Products = await db.products
            .Include(p => p.brand)
            .Include(p => p.category)
            .OrderBy(p => p.product_id)
            .ToListAsync();

 
        ViewBag.Brands = await db.brands.OrderBy(b => b.brand_name).ToListAsync();
        ViewBag.Categories = await db.categories.OrderBy(c => c.category_name).ToListAsync();
        ViewBag.Stores = await db.stores.OrderBy(s => s.store_name).ToListAsync();
        ViewBag.AllStaff = await db.staffs.OrderBy(s => s.first_name).ToListAsync(); 

   
        var productImages = new Dictionary<int, string>();
        string imagesFolder = HostingEnvironment.MapPath("~/Bikes_Images/");

        foreach (var p in ViewBag.Products)
        {
            string brandClean = (p.brand?.brand_name ?? "Unknown").Replace(" ", "_");
            string found = Url.Content("~/Bikes_Images/default_bike.jpeg");

            if (!string.IsNullOrEmpty(imagesFolder))
            {
                var candidates = new[]
                {
                    $"{brandClean}_bike.jpeg", $"{brandClean}_bike.jpg",
                    $"{brandClean}1.jpeg", $"{brandClean}.jpeg"
                };

                foreach (var c in candidates)
                {
                    var full = System.IO.Path.Combine(imagesFolder, c);
                    if (System.IO.File.Exists(full))
                    {
                        found = Url.Content("~/Bikes_Images/" + c);
                        break;
                    }
                }
            }

            productImages[p.product_id] = found;
        }

        ViewBag.ProductImages = productImages;

        return View();
    }


    [HttpPost]
    public async Task<ActionResult> EditStaff(staff updated)
    {
        try
        {
            if (ModelState.IsValid)
            {
                db.Entry(updated).State = EntityState.Modified;
                await db.SaveChangesAsync();
                TempData["Success"] = "Staff updated successfully!";
            }
            else
            {
                TempData["Error"] = "Invalid data provided.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error updating staff: " + ex.Message;
        }
        return RedirectToAction("Maintain");
    }
    public async Task<ActionResult> DeleteStaff(int id)
    {
        try
        {
            var staff = await db.staffs.FindAsync(id);
            if (staff != null)
            {
                db.staffs.Remove(staff);
                await db.SaveChangesAsync();
                TempData["Success"] = "Staff deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Staff not found.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error deleting staff: " + ex.Message;
        }
        return RedirectToAction("Maintain");
    }

 
    [HttpPost]
    public async Task<ActionResult> EditCustomer(customer updated)
    {
        try
        {
            if (ModelState.IsValid)
            {
                db.Entry(updated).State = EntityState.Modified;
                await db.SaveChangesAsync();
                TempData["Success"] = "Customer updated successfully!";
            }
            else
            {
                TempData["Error"] = "Invalid data provided.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error updating customer: " + ex.Message;
        }
        return RedirectToAction("Maintain");
    }


    public async Task<ActionResult> DeleteCustomer(int id)
    {
        try
        {
            var customer = await db.customers.FindAsync(id);
            if (customer != null)
            {
                db.customers.Remove(customer);
                await db.SaveChangesAsync();
                TempData["Success"] = "Customer deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Customer not found.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error deleting customer: " + ex.Message;
        }
        return RedirectToAction("Maintain");
    }

    [HttpPost]
    public async Task<ActionResult> EditProduct(product updated)
    {
        try
        {
            if (ModelState.IsValid)
            {
                db.Entry(updated).State = EntityState.Modified;
                await db.SaveChangesAsync();
                TempData["Success"] = "Product updated successfully!";
            }
            else
            {
                TempData["Error"] = "Invalid data provided.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error updating product: " + ex.Message;
        }
        return RedirectToAction("Maintain");
    }

    public async Task<ActionResult> DeleteProduct(int id)
    {
        try
        {
            var product = await db.products.FindAsync(id);
            if (product != null)
            {
                db.products.Remove(product);
                await db.SaveChangesAsync();
                TempData["Success"] = "Product deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Product not found.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Error deleting product: " + ex.Message;
        }
        return RedirectToAction("Maintain");
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