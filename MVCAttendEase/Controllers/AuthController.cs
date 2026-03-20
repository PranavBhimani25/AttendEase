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
using System.Security.Cryptography;

namespace MVCAttendEase.Controllers
{
    // [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly CloudinaryService _cloudinaryService;
        private readonly MailService _mailService;
        private readonly IAuthInterface _auth;
        private readonly ILogger<AuthController> _logger;
        private readonly ElasticsearchService _elastic;

        public AuthController(ILogger<AuthController> logger, IAuthInterface auth, CloudinaryService cloudinaryService, MailService mailService, ElasticsearchService elastic)
        {
            _logger = logger;
            _auth = auth;
            _cloudinaryService = cloudinaryService;
            _mailService = mailService;
            _elastic = elastic;
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

            var employeeId = await _auth.Register(model);
            if(employeeId > 0)
            {
                try
                {
                    var employeeDoc = new EmployeeIndexDocument
                    {
                        EmpId = employeeId,
                        Name = model.c_name,
                        Email = model.c_email,
                        Gender = model.c_gender,
                        Status = "Active",
                        Role = "Employee",
                        TotalWorkingHours = 0,
                        TotalDaysPresent = 0,
                        LateInCount = 0,
                        EarlyOutCount = 0,
                        LastAttendDate = DateTime.Now
                    };

                    await _elastic.IndexWithIdAsync("employee_index", employeeId.ToString(), employeeDoc);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Employee {EmpId} created in DB but failed to index in Elasticsearch", employeeId);
                }

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

                try
                {
                    _mailService.SendEmail(model.c_email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Employee {EmpId} registered but welcome email could not be sent", employeeId);
                }

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




        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgetPassword(string email, string otp)
        {
           if (email == null)
            {
                return BadRequest(new { message = "Invalid Email request", success = false });
            }


            var storedOtp = HttpContext.Session.GetString("OTP");
            var storedTime = HttpContext.Session.GetString("OTP_Time");
            var storedEmail = HttpContext.Session.GetString("Email");
            
            DateTime otpTime = DateTime.Parse(storedTime);
            // ⏱️ Check 1 minute expiry
            if ((DateTime.Now - otpTime).TotalMinutes > 1)
            {
                return Json(new { success = false, message = "OTP expired" });
            }


            if(!(storedOtp == otp && storedEmail == email))
            {
                return Ok(new { message = "OTP Not Matched", success = false });
            }

            HttpContext.Session.Clear();

            int length = 6;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var result = new char[length];

            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] buffer = new byte[length];
                rng.GetBytes(buffer);

                for (int i = 0; i < length; i++)
                {
                    result[i] = chars[buffer[i] % chars.Length];
                }
            }
            string password = new string(result);

            LoginModel ForgetPassword = new LoginModel()
            {
                c_email = email,
                c_password = password
            };

            var status = await _auth.ForgetPassword(ForgetPassword);


            if (status == -1)
            {
                return Ok(new { message = "Email Not Exist. Register First",success=false });
            }
            else if(status == 1)
            {
                
                string body = $@"Dear {ForgetPassword.c_email}, <br><br>

                                Your password has been successfully reset. 🔐   <br><br>

                                You can now log in to your account using your new password <b> {ForgetPassword.c_password} <b>.   <br><br>

                                For security reasons, we recommend that you change this password after logging in.   <br><br>

                                If you did not request this change, please contact our support team immediately.    <br><br>

                                Best regards,
                                Admin Team
                                AttendEase System";
                string subject = "Password Reset Successfully";


                _mailService.SendEmail(ForgetPassword.c_email, subject, body);
                return Ok(new { message = "New Pasword Sent to Your Email....",success=true });
            }
            else
            {
                return BadRequest(new { message = "New Password Not Set", success = false });
            }
        }




        [HttpPost]
        public async Task<IActionResult> SendOtp(string email)
        {
            if(email == null)
            {
                return BadRequest(new { message = "Email is Required", success = false });
            }
            int status = await _auth.GetOne(email);
            if(status == 0)
            {
                return BadRequest(new { message = "User Not Exists", success = false });
            }
            var otp = new Random().Next(100000, 999999).ToString();

            // Save OTP (DB / Redis recommended)
            HttpContext.Session.SetString("OTP", otp);
            HttpContext.Session.SetString("OTP_Time", DateTime.Now.ToString());
            HttpContext.Session.SetString("Email", email);

            string path = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "OtpVerify.html");
            string body = System.IO.File.ReadAllText(path);

            // Replace dynamic values
            body = body.Replace("000000", otp);
            body = body.Replace("username", email);


            // Send Email
            _mailService.SendEmail(email, "OTP Verification", body);

            return Json(new { success = true });
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
