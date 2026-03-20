using Microsoft.AspNetCore.Mvc;
using MVCAttendEase.Models;
using MVCAttendEase.Services;
using Repositories.Interfaces;
using Repositories.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace MVCAttendEase.Controllers
{
    public class AuthController : Controller
    {
        private readonly CloudinaryService _cloudinaryService;
        private readonly MailService _mailService;
        private readonly IAuthInterface _auth;
        private readonly ILogger<AuthController> _logger;
        private readonly ElasticsearchService _elastic;
        private readonly NotificationPublisher _notificationPublisher;
        private readonly RedisService _redis;

        public AuthController(
            ILogger<AuthController> logger,
            IAuthInterface auth,
            CloudinaryService cloudinaryService,
            MailService mailService,
            NotificationPublisher notificationPublisher,
            ElasticsearchService elastic,
            RedisService redis)
        {
            _logger = logger;
            _auth = auth;
            _cloudinaryService = cloudinaryService;
            _redis = redis;
            _mailService = mailService;
            _elastic = elastic;
            _notificationPublisher = notificationPublisher;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GET: /Auth/Login
        // ─────────────────────────────────────────────────────────────────────
        public IActionResult Login()
        {
            return View();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  POST: /Auth/Login
        //  Flow:
        //    1. Check Redis cache (fast — for recently registered users)
        //    2. If not in Redis → check PostgreSQL (normal flow)
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (model == null)
                return BadRequest(new { message = "Invalid login request", success = false });

            // ── STEP 1: Check Redis first ─────────────────────────────────────
            var redisKey = $"employee:{model.c_email}";
            var cachedData = await _redis.GetAsync(redisKey);

            if (cachedData != null)
            {
                var cachedEmployee = JsonSerializer.Deserialize<EmployeeModel>(cachedData);

                if (cachedEmployee != null && cachedEmployee.Password == model.c_password)
                {
                    // Redis hit — login instantly without hitting PostgreSQL
                    HttpContext.Session.SetString("Role", cachedEmployee.Role?.Trim() ?? "Employee");
                    HttpContext.Session.SetString("empId", cachedEmployee.EmpId.ToString());

                    _logger.LogInformation("Login via Redis cache for: {Email}", model.c_email);

                    return Ok(new
                    {
                        message = "Login successful",
                        success = true,
                        role = cachedEmployee.Role,
                        id = cachedEmployee.EmpId
                    });
                }
            }

            // ── STEP 2: Redis miss — check PostgreSQL ─────────────────────────
            var result = await _auth.Login(model);

            if (result == null)
                return BadRequest(new { message = "Invalid email or password", success = false });

            if (result.Status != "Active")
                return BadRequest(new { message = "Account is not active. Please contact admin.", success = false });

            // Cache successful login in Redis so next login is served from cache
            var employeeCache = new EmployeeModel
            {
                EmpId = result.EmpId,
                Name = string.Empty,
                Email = result.Email ?? model.c_email,
                Password = model.c_password,
                Gender = string.Empty,
                Status = result.Status,
                Role = result.Role,
                ProfileImage = string.Empty
            };

            var jsonData = JsonSerializer.Serialize(employeeCache);
            await _redis.SetAsync(redisKey, jsonData, TimeSpan.FromMinutes(30));

            HttpContext.Session.SetString("Role", result.Role?.Trim() ?? string.Empty);
            HttpContext.Session.SetString("empId", result.EmpId.ToString());

            _logger.LogInformation("Login via PostgreSQL for: {Email}", model.c_email);

            return Ok(new
            {
                message = "Login successful",
                success = true,
                role = result.Role,
                id = result.EmpId
            });
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GET: /Auth/Register
        // ─────────────────────────────────────────────────────────────────────
        public IActionResult Register()
        {
            return View();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  POST: /Auth/Register
        //  Flow:
        //    1. Upload profile image to Cloudinary (if provided)
        //    2. Save employee to PostgreSQL (permanent storage)
        //    3. Also save to Redis for 30 minutes (fast login cache)
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Register([FromForm] RegisterEmployeeModel model)
        {
            if (model == null)
            {
                return BadRequest(new { message = "Invalid registration request", success = false });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid model data", success = false });
            }

            try
            {
                // ✅ Upload Profile Image
                if (model.c_profileimage != null && model.c_profileimage.Length > 0)
                {
                    model.ProfileImageUrl = await _cloudinaryService.UploadImageAsync(model.c_profileimage);
                }

                // ✅ Register User
                var employeeId = await _auth.Register(model);

                if (employeeId <= 0)
                {
                    return BadRequest(new { message = "Registration failed", success = false });
                }
                if (employeeId > 0)
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

                    // ✅ Safe Name Handling
                    var userDisplayName = string.IsNullOrWhiteSpace(model.c_name)
                        ? "Employee"
                        : model.c_name;

                    // ✅ Send Email (ONLY if email exists)
                    if (!string.IsNullOrWhiteSpace(model.c_email))
                    {
                        string body = $@"Dear {userDisplayName}, <br><br>
                                    Thank you for registering with our Attendance Management System. <br>
                                    Your registration has been completed successfully. 🎉 <br><br>

                                    Please note that your account is currently under review. 
                                    You will be able to login only after an administrator activates your account. <br><br>

                                    Once your account is activated, you will receive a confirmation. <br><br>

                                    Best regards, <br>
                                    Admin Team <br>
                                    AttendEase System";

                        string subject = "Registration Successful";

                        try
                        {
                            _mailService.SendEmail(model.c_email, subject, body);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Email sending failed for {Email}", model.c_email);
                        }
                    }


                    // ✅ Publish Notification (RabbitMQ)
                    var notification = new NotificationMessage
                    {
                        EmployeeId = employeeId,
                        FullName = userDisplayName,
                        Email = model.c_email ?? string.Empty,
                        Role = "Employee",
                        Message = $"New user registration from {userDisplayName}.",
                        RegisteredAt = DateTime.UtcNow
                    };

                    try
                    {
                        await _notificationPublisher.PublishAsync(notification);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to publish notification for {Email}", model.c_email);
                    }

                    return Ok(new
                    {
                        message = "Registration successful",
                        success = true,
                        employeeId = employeeId
                    });

                    // return Ok(new { message = "Registration successful", success = true });
                }
                else
                {
                    return BadRequest(new { message = "Registration failed", success = false });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");

                return StatusCode(500, new
                {
                    message = "Something went wrong",
                    success = false
                });
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  GET: /Auth/Logout
        // ─────────────────────────────────────────────────────────────────────
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


            if (!(storedOtp == otp && storedEmail == email))
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
                return Ok(new { message = "Email Not Exist. Register First", success = false });
            }
            else if (status == 1)
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
                return Ok(new { message = "New Pasword Sent to Your Email....", success = true });
            }
            else
            {
                return BadRequest(new { message = "New Password Not Set", success = false });
            }
        }




        [HttpPost]
        public async Task<IActionResult> SendOtp(string email)
        {
            if (email == null)
            {
                return BadRequest(new { message = "Email is Required", success = false });
            }
            int status = await _auth.GetOne(email);
            if (status == 0)
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
