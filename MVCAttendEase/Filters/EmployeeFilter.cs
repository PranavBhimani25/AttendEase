using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MVCAttendEase.Filters
{
    public class EmployeeFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
            
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("Role");
            if (role != "Employee")
            {
                context.Result = new Microsoft.AspNetCore.Mvc.RedirectToActionResult("Login", "Auth", null);
            }
        }
    }
}