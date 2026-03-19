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
        private readonly MailService _mailService;
        private readonly IAuthInterface _auth;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ILogger<AuthController> logger, IAuthInterface auth, CloudinaryService cloudinaryService, MailService mailService)
        {
            _logger = logger;
            _auth = auth;
            _cloudinaryService = cloudinaryService;
            _mailService = mailService;
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
                return BadRequest(new { message = "Invalid login request", success = false });
            }

            var result = await _auth.Login(model);


            if (result == null)
            {
                return BadRequest(new { message = "Invalid email or password", success = false });
            }

            if (result.Status != "Active")
            {
                return BadRequest(new { message = "Account is not active", success = false });
            }

                HttpContext.Session.SetString("Role", result.Role?.Trim() ?? string.Empty);
                HttpContext.Session.SetString("empId", result.EmpId.ToString());
                return Ok(new { message = "Login successful",success=true , role = result.Role, id = result.EmpId });
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
                string body = $@"Dear {model.c_name}, <br><br>

                                    Thank you for registering with our Attendance Management System. <br>
                                    Your registration has been completed successfully. 🎉 <br><br>

                                    Please note that your account is currently under review. 
                                    You will be able to login only after an administrator activates your account. <br><br>

                                    Once your account is activated, you will receive a confirmation, 
                                    and then you can access the system using your credentials. <br><br>

                                    If you have any questions or need assistance, feel free to contact us. <br><br>

                                    Best regards, <br>
                                    Admin Team <br>
                                    AttendEase System
                                    ";
                string subject = "Registration Successfully";

                _mailService.SendEmail(model.c_email, subject, body);

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
