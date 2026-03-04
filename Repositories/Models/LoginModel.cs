using System.ComponentModel.DataAnnotations;

namespace Repositories.Models
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string c_email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string c_password { get; set; }
    }
}