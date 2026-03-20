using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class SendPdfModel
    {
        public int EmpId { get; set; }
        public string FileName { get; set; }
        public string FileData { get; set; } // Base64
    }
}