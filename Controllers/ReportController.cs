using Microsoft.AspNetCore.Mvc;
using ggy_app_service_account_report.Services;
using System.Collections.Generic;
using System.Linq;

namespace AccountStatusClass.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly AccountStatusService _accountStatusService;

        public ReportController(AccountStatusService accountStatusService)
        {
            _accountStatusService = accountStatusService;
        }

        [HttpGet]
        public IActionResult GenerateReport()
        {
            var users = new List<string> { "GGYGSDAdmin01", "GGYGSDAdmin02" }; // Replace with actual user list
            var servers = new List<string> { "AZWAPPGFAEGRS01", "AZWAPPGFAEGRS02" }; // Replace with actual server list
            var domain = "mfcgd.com"; // Replace with the actual domain

            var userStatuses = _accountStatusService.GetADUserStatus(users,domain);
            var serverStatuses = servers.Select(server => new
            {
                Server = server,
                IsPortOpen = _accountStatusService.IsPortOpen(server, 3389),
                LoggedInUsers = _accountStatusService.GetLoggedInUsers(server)
            }).ToList();
             // Define CSS styles
            var cssStyles = @"
                <style>
                    body { font-family: Arial, sans-serif; }
                    h1 { color: #333; }
                    h2 { color: #555; }
                    ul { list-style-type: none; padding: 0; }
                    li { margin-bottom: 10px; }
                    .user-status, .server-status { margin-bottom: 20px; }
                    .server-status ul { margin-left: 20px; }
                </style>";

            // Generate HTML report (simplified for brevity)
            var reportHtml = "<html><head>" + cssStyles + "</head><body><h1>Account Status Report</h1>";
            reportHtml += "<div class='user-status'><h2>User Status</h2><ul>";
            foreach (var status in userStatuses)
            {
                reportHtml += $"<li>{status.DisplayName} - Locked Out: {status.LockedOut} - Last Logon: {status.LastLogon}</li>";
            }
            reportHtml += "</ul></div><div class='server-status'><h2>Server Status</h2><ul>";
            foreach (var status in serverStatuses)
            {
                reportHtml += $"<li>{status.Server} - Port 3389 Open: {status.IsPortOpen}</li>";
                reportHtml += "<ul>";
                foreach (var session in status.LoggedInUsers)
                {
                    reportHtml += $"<li>{session.UserName} - Logon Time: {session.LogonTime}</li>";
                }
                reportHtml += "</ul>";
            }
            reportHtml += "</ul></div></body></html>";

            return Content(reportHtml, "text/html");
        }
    }
}