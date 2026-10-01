using BeeDienLanh.Data;
using BeeDienLanh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeeDienLanh.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // ==========================================
        // CONSTRUCTOR
        // ==========================================
        public ProductsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ==========================================
        // HÀM LƯU ẢNH SẢN PHẨM
        // ==========================================
        private async Task<string?> SaveImageAsync(IFormFile? imageFile)
        {
            // Không có ảnh
            if (imageFile == null || imageFile.Length == 0)
            {
                return null;
            }

            // Các định dạng ảnh được phép
            string[] allowedExtensions =
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            string extension =
                Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            // Kiểm tra định dạng
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException(
                    "Chỉ được chọn ảnh JPG, JPEG, PNG hoặc WEBP.");
            }

            // Giới hạn 5MB
            if (imageFile.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException(
                    "Dung lượng ảnh không được lớn hơn 5MB.");
            }

            // Đường dẫn thư mục:
            // wwwroot/images/products
            string folderPath = Path.Combine(
                _environment.WebRootPath,
                "images",
                "products");

            // Nếu thư mục chưa tồn tại thì tự tạo
            Directory.CreateDirectory(folderPath);

            // Tạo tên file ngẫu nhiên để tránh trùng
            string fileName =
                Guid.NewGuid().ToString() + extension;

            // Đường dẫn vật lý của file
            string filePath =
                Path.Combine(folderPath, fileName);

            // Lưu file
            using (var stream =
                   new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Đường dẫn lưu trong SQL Server
            return "/images/products/" + fileName;
        }

        // ==========================================
        // DANH SÁCH SẢN PHẨM
        // URL: /Admin/Products
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var products = await _context.Products
                .OrderByDescending(p => p.ProductId)
                .ToListAsync();

            return View(products);
        }

        // ==========================================
        // THÊM SẢN PHẨM - GET
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // ==========================================
        // THÊM SẢN PHẨM - POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Product product,
            IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Lưu ảnh
                    string? imagePath =
                        await SaveImageAsync(imageFile);

                    // Nếu Admin có chọn ảnh
                    if (!string.IsNullOrEmpty(imagePath))
                    {
                        product.ImageUrl = imagePath;
                    }

                    // Thêm sản phẩm vào database
                    _context.Products.Add(product);

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Thêm sản phẩm thành công!";

                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(
                        "imageFile",
                        ex.Message);
                }
            }

            return View(product);
        }

        // ==========================================
        // SỬA SẢN PHẨM - GET
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(
                    p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // ==========================================
        // SỬA SẢN PHẨM - POST
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            Product product,
            IFormFile? imageFile)
        {
            if (id != product.ProductId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy sản phẩm cũ trong database
                    var existingProduct =
                        await _context.Products
                            .FirstOrDefaultAsync(
                                p => p.ProductId == id);

                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // ==================================
                    // CẬP NHẬT THÔNG TIN
                    // ==================================
                    existingProduct.ProductName =
                        product.ProductName;

                    existingProduct.Price =
                        product.Price;

                    existingProduct.StockQuantity =
                        product.StockQuantity;

                    existingProduct.Description =
                        product.Description;

                    existingProduct.CategoryId =
                        product.CategoryId;

                    existingProduct.IsFeatured =
                        product.IsFeatured;

                    // ==================================
                    // XỬ LÝ ẢNH
                    // ==================================

                    // Nếu Admin chọn ảnh mới
                    if (imageFile != null &&
                        imageFile.Length > 0)
                    {
                        string? newImagePath =
                            await SaveImageAsync(imageFile);

                        if (!string.IsNullOrEmpty(
                                newImagePath))
                        {
                            existingProduct.ImageUrl =
                                newImagePath;
                        }
                    }

                    // Nếu không chọn ảnh mới:
                    // existingProduct.ImageUrl
                    // vẫn giữ nguyên ảnh cũ.

                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] =
                        "Cập nhật sản phẩm thành công!";

                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(
                        "imageFile",
                        ex.Message);
                }
            }

            return View(product);
        }

        // ==========================================
        // XÓA SẢN PHẨM - GET
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(
                    p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // ==========================================
        // XÓA SẢN PHẨM - POST
        // ==========================================
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(
                    p => p.ProductId == id);

            if (product != null)
            {
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Xóa sản phẩm thành công!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}