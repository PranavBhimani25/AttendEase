using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Repositories.Models
{
    public class RegisterEmployeeModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        public string c_name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string c_email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6)]
        public string c_password { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string c_gender { get; set; }

        public IFormFile? c_profileimage { get; set; }
        public string? ProfileImageUrl { get; set; }
    }
}