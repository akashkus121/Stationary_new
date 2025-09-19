using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stationary.Data;
using Stationary.Models;
using System.Linq;

namespace Stationary.Controllers
{
    public class AccountController : Controller
    {

        private readonly ApplicationDbContext _db;

       

        public AccountController(ApplicationDbContext db)
        {
            _db = db;
           

        }

        public IActionResult Login(string role)
        {
            ViewBag.Role = role;
            return View(); // Looks for Views/Account/Login.cshtml
        }

        [HttpPost]
        public IActionResult Login(string username, string password, string role)
        {
            var user = _db.Users.FirstOrDefault(u =>
                u.Username == username &&
                u.Password == password &&
                u.Role == role);

            if (user != null)
            {
                // ✅ Store UserId in session
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                // ✅ Clear cart after login
                var cartItems = _db.Carts.Where(c => c.UserId == user.Id);
                if (cartItems.Any())
                {
                    _db.Carts.RemoveRange(cartItems);
                    _db.SaveChanges();
                }

                if (user.Role == "Admin")
                    return RedirectToAction("Products", "Admin");
                else
                    return RedirectToAction("Index", "User");
            }

            ViewBag.Error = "Invalid credentials or role.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
