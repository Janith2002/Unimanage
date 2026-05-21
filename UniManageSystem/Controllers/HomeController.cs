using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UniManageSystem.Models;

namespace UniManageSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private static bool _isFirstLoad = true;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (_isFirstLoad)
            {
                TempData["SuccessMessage"] = "Database connected successfully!";
                _isFirstLoad = false;
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
