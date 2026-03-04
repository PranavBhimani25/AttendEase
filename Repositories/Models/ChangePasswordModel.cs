using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class ChangePasswordModel
    {
        public string c_email { get; set; }
        public string c_currentPassword { get; set; }
        public string c_newPassword { get; set; }
    }
}