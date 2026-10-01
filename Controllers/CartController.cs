using System.Text.Json;
using BeeDienLanh.Data;
using BeeDienLanh.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeeDienLanh.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        private const string CartSessionKey = "BeeCart";

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // XEM GIỎ HÀNG
        // ==========================================
        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCart();

            return View(cart);
        }

        // ==========================================
        // THÊM SẢN PHẨM VÀO GIỎ
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            var cart = GetCart();

            var existingItem = cart
                .FirstOrDefault(x => x.ProductId == id);

            if (existingItem == null)
            {
                cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Quantity = 1,
                    ImageUrl = product.ImageUrl
                });
            }
            else
            {
                existingItem.Quantity++;
            }

            SaveCart(cart);

            // Ép Session ghi dữ liệu ngay
            await HttpContext.Session.CommitAsync();

            TempData["SuccessMessage"] =
                $"Đã thêm {product.ProductName} vào giỏ hàng.";

            return Redirect("/Cart");
        }

        // ==========================================
        // TĂNG SỐ LƯỢNG
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Increase(int id)
        {
            var cart = GetCart();

            var item = cart
                .FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                item.Quantity++;

                SaveCart(cart);

                await HttpContext.Session.CommitAsync();
            }

            return Redirect("/Cart");
        }

        // ==========================================
        // GIẢM SỐ LƯỢNG
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decrease(int id)
        {
            var cart = GetCart();

            var item = cart
                .FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                item.Quantity--;

                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }

                SaveCart(cart);

                await HttpContext.Session.CommitAsync();
            }

            return Redirect("/Cart");
        }

        // ==========================================
        // XÓA SẢN PHẨM
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var cart = GetCart();

            var item = cart
                .FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                cart.Remove(item);

                SaveCart(cart);

                await HttpContext.Session.CommitAsync();
            }

            return Redirect("/Cart");
        }

        // ==========================================
        // ĐỌC GIỎ HÀNG TỪ SESSION
        // ==========================================
        private List<CartItem> GetCart()
        {
            var cartJson =
                HttpContext.Session.GetString(CartSessionKey);

            if (string.IsNullOrWhiteSpace(cartJson))
            {
                return new List<CartItem>();
            }

            try
            {
                return JsonSerializer
                    .Deserialize<List<CartItem>>(cartJson)
                    ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        // ==========================================
        // LƯU GIỎ HÀNG VÀO SESSION
        // ==========================================
        private void SaveCart(List<CartItem> cart)
        {
            var cartJson =
                JsonSerializer.Serialize(cart);

            HttpContext.Session.SetString(
                CartSessionKey,
                cartJson);
        }
    }
}