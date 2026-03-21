using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Models;
public class MsgToEmp
{
	public string Sender {get; set;}
    public string Receiver { get; set; }
    public string Message { get; set; }
    public DateTime CreatedAt { get; set; }
}
