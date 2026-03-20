using Microsoft.AspNetCore.Mvc;
using MVCAttendEase.Services;
using Repositories.Interfaces;
using Repositories.Models;
using System.Text.Json;

namespace MVCAttendEase.Controllers
{
    public class AuthController : Controller
    {
        private readonly CloudinaryService _cloudinaryService;
        private readonly MailService _mailService;
        private readonly IAuthInterface _auth;
        private readonly ILogger<AuthController> _logger;
        private readonly RedisService _redis;

        public AuthController(
            ILogger<AuthController> logger,
            IAuthInterface auth,
            CloudinaryService cloudinaryService,
            RedisService redis, MailService mailService)
        {
            _logger    = logger;
            _auth      = auth;
            _cloudinaryService = cloudinaryService;
            _redis     = redis;
            _mailService = mailService;
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
            var redisKey    = $"employee:{model.c_email}";
            var cachedData  = await _redis.GetAsync(redisKey);

            if (cachedData != null)
            {
                var cachedEmployee = JsonSerializer.Deserialize<EmployeeModel>(cachedData);

                if (cachedEmployee != null && cachedEmployee.Password == model.c_password)
                {
                    // Redis hit — login instantly without hitting PostgreSQL
                    HttpContext.Session.SetString("Role",  cachedEmployee.Role?.Trim() ?? "Employee");
                    HttpContext.Session.SetString("empId", cachedEmployee.EmpId.ToString());

                    _logger.LogInformation("Login via Redis cache for: {Email}", model.c_email);

                    return Ok(new
                    {
                        message = "Login successful",
                        success = true,
                        role    = cachedEmployee.Role,
                        id      = cachedEmployee.EmpId
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
                EmpId        = result.EmpId,
                Name         = string.Empty,
                Email        = result.Email ?? model.c_email,
                Password     = model.c_password,
                Gender       = string.Empty,
                Status       = result.Status,
                Role         = result.Role,
                ProfileImage = string.Empty
            };

            var jsonData = JsonSerializer.Serialize(employeeCache);
            await _redis.SetAsync(redisKey, jsonData, TimeSpan.FromMinutes(30));

            HttpContext.Session.SetString("Role",  result.Role?.Trim() ?? string.Empty);
            HttpContext.Session.SetString("empId", result.EmpId.ToString());

            _logger.LogInformation("Login via PostgreSQL for: {Email}", model.c_email);

            return Ok(new
            {
                message = "Login successful",
                success = true,
                role    = result.Role,
                id      = result.EmpId
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
