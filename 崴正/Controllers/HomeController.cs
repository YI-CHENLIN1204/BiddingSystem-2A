using 崴正.Data;
using 崴正.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace 崴正.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Error()
    {
        return View();
    }
    public IActionResult EstimationUI()
    {
        return View();
    }
    public IActionResult UploadFinalBid()
    {
        return View();
    }
}