﻿using Microsoft.AspNetCore.Mvc;

namespace Orneholm.BirdOrNot.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
