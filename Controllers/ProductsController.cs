using BeeDienLanh.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeeDienLanh.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sort)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            // TÌM KIẾM ĐƠN GIẢN
            if (!string.IsNullOrWhiteSpace(search))
            {
                products = products.Where(p =>
                    p.ProductName.Contains(search) ||
                    (p.Description != null &&
                     p.Description.Contains(search)));
            }

            // LỌC THEO DANH MỤC
            if (categoryId.HasValue)
            {
                products = products.Where(p =>
                    p.CategoryId == categoryId.Value);
            }

            // TÌM KIẾM NÂNG CAO - GIÁ TỪ
            if (minPrice.HasValue)
            {
                products = products.Where(p =>
                    p.Price >= minPrice.Value);
            }

            // TÌM KIẾM NÂNG CAO - GIÁ ĐẾN
            if (maxPrice.HasValue)
            {
                products = products.Where(p =>
                    p.Price <= maxPrice.Value);
            }

            // SẮP XẾP
            switch (sort)
            {
                case "price_asc":
                    products = products.OrderBy(p => p.Price);
                    break;

                case "price_desc":
                    products = products.OrderByDescending(p => p.Price);
                    break;

                case "name_asc":
                    products = products.OrderBy(p => p.ProductName);
                    break;

                default:
                    products = products.OrderByDescending(p => p.ProductId);
                    break;
            }

            // Gửi danh mục sang View để làm dropdown
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            // Giữ lại dữ liệu user vừa nhập
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Sort = sort;

            return View(await products.ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}