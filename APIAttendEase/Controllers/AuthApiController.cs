using Microsoft.AspNetCore.Mvc;
using Repositories.Interfaces;
using Repositories.Models;
using APIAttendEase.Services;

namespace APIAttendEase.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthApiController : ControllerBase
    {
        private readonly IAuthInterface _authService;
        private readonly JwtService _jwtService;
        private readonly CloudinaryService _cloudinaryService;
        public AuthApiController(IAuthInterface authService, JwtService jwtService, CloudinaryService cloudinaryService)
        {
            _authService = authService;
            _jwtService = jwtService;
            _cloudinaryService = cloudinaryService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var result = await _authService.Login(model);
            if (result != null)
            {
                var token = _jwtService.GenerateToken(result.c_empid, result.c_email, result.c_role);
                return Ok(new { token, Message = "Login successful", Role = result.c_role });
            }
            else
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }
        }

        // [HttpPost("register")]
        // public async Task<IActionResult> Register([FromForm] RegisterEmployeeModel model)
        // {
        //     var imageUrl = _cloudinaryService.UploadImageAsync(model.c_profileimage);

        //     model.ProfileImageUrl = imageUrl.Result;
            
        //     var result = await _authService.Register(model);
             
        //     if (result > 0)
        //     {
        //         return Ok(new { Message = "Registration successful", success=true });
        //     }
        //     else
        //     {
        //         return BadRequest(new { Message = "Registration failed", success= false });
        //     }
        // }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterEmployeeModel model)
        {
            if (model.c_profileimage != null && model.c_profileimage.Length > 0)
            {
                model.ProfileImageUrl = await _cloudinaryService.UploadImageAsync(model.c_profileimage);
            }

            var result = await _authService.Register(model);

            if (result > 0)
            {
                return Ok(new { Message = "Registration successful", success = true });
            }
            else
            {
                return BadRequest(new { Message = "Registration failed", success = false });
            }
        }


    }
}