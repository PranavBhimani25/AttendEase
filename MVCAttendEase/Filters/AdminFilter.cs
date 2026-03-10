using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MVCAttendEase.Filters
{
    public class AdminFilter : IActionFilter
    {
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("Role");
            var empId = context.HttpContext.Session.GetString("empId");
            if (role != "Admin")
            {
                context.Result = new Microsoft.AspNetCore.Mvc.RedirectToActionResult("Login", "Auth", null);
            }
        }
    }
}