using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MVCAttendEase.Models;
using MVCAttendEase.Services;
using Repositories.Interfaces;
using Repositories.Models;

namespace MVCAttendEase.Controllers
{
    // [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly CloudinaryService _cloudinaryService;
        private readonly IAuthInterface _auth;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger, IAuthInterface auth, CloudinaryService cloudinaryService)
        {
            _logger = logger;
            _auth = auth;
            _cloudinaryService = cloudinaryService;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null)
            {
                return BadRequest("Invalid login request");
            }

            var result = await _auth.Login(model);

            if (result != null)
            {
                HttpContext.Session.SetString("Role", result.Role?.Trim() ?? string.Empty);
                HttpContext.Session.SetString("empId", result.EmpId.ToString());
                return Ok(new { message = "Login successful",success=true , role = result.Role, id = result.EmpId });
            }
            return BadRequest(new { message = "Invalid email or password", success = false });
        }
      
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromForm] RegisterEmployeeModel model)
        {
            if(model == null)
            {
                return BadRequest("Invalid registration request");
            }

            if (model.c_profileimage != null && model.c_profileimage.Length > 0)
            {
                model.ProfileImageUrl = await _cloudinaryService.UploadImageAsync(model.c_profileimage);
            }

            var employee = await _auth.Register(model);
            if(employee > 0)
            {
                return Ok(new { message = "Registration successful", success = true });
            }
            else
            {
                return BadRequest(new { message = "Registration failed", success = false });
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login", "Auth");
        }

        public IActionResult SetSession(string role)
        {
            HttpContext.Session.SetString("Role", role);
            return Ok();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
