using Microsoft.AspNetCore.Mvc;
using ggy_app_service_account_report.Services;
using AccountStatusClass.Services;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AccountStatusClass.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly AccountStatusService _accountStatusService;
        private readonly ConfigurationService _configurationService;
        private readonly ServerUserService _serverUserService;

        public ReportController(AccountStatusService accountStatusService, ConfigurationService configurationService, ServerUserService serverUserService)
        {
            _accountStatusService = accountStatusService;
            _configurationService = configurationService;
            _serverUserService = serverUserService;
        }

        [HttpGet]
        public IActionResult GenerateReport()
        {
            var config = _configurationService.GetConfiguration();
            if (config == null)
            {
                return BadRequest("Failed to load configuration.");
            }
            var userGroups = config.UserGroups; //new List<string> { "GGYGSDAdmin01", "GGYGSDAdmin02" }; // Replace with actual user list
            var servers = config.Servers; //new List<string> { "AZWAPPGFAEGRS01", "AZWAPPGFAEGRS02" }; // Replace with actual server list
            var domain = "mfcgd.com"; // Replace with the actual domain

            var userStatuses = userGroups.SelectMany(group => _accountStatusService.GetADUserStatus(group.Value, domain)).ToList();
            var serverStatuses = servers.Select(server => new
            {
                Server = server,
                IsPortOpen = _accountStatusService.IsPortOpen(server, 3389),
                LoggedInUsers = _serverUserService.GetLoggedInUsers(server, userGroups.SelectMany(g => g.Value).ToList()) // Pass user list
            }).ToList();
             // Define CSS styles
            var cssStyles = @"
                <style>
    h1 { font-family: Arial, Helvetica, sans-serif;
        color: #e68a00;
        font-size: 20px;}
    h2 {font-family: Arial, Helvetica, sans-serif;
        color: #000099;
        font-size: 16px;}
   table {
		font-size: 12px;
		border: 0px; 
		font-family: Arial, Helvetica, sans-serif;} 
    td {
		padding: 4px;
		margin: 0px;
		border: 0;}
    th {
        background: #395870;
        background: linear-gradient(#49708f, #293f50);
        color: #fff;
        font-size: 11px;
        text-transform: uppercase;
        padding: 10px 15px;
        vertical-align: middle; }
    tbody tr:nth-child(even) {
    background: #f0f0f2;}
    #CreationDate { 
        font-family: Arial, Helvetica, sans-serif;
        color: #ff3300;
        font-size: 12px;}
    .LockedStatus { color: #ff0000; font-weight: bold;}
    .OkStatus { color: #008000; font-weight: bold; }
                </style>";

            // Generate HTML report (simplified for brevity)
            var sb = new StringBuilder();
            sb.AppendLine( "<html><head>" + cssStyles + "</head><body><h1>GAITS System Account Status Report</h1>");
            foreach (var group in userGroups)
            {
                sb.AppendLine($"<h2>{group.Key} - System Account Status</h2>");
                var groupUserStatuses = _accountStatusService.GetADUserStatus(group.Value, domain);
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>DisplayName</th><th>LockedOut</th><th>LastLogonDate</th><th>AccountLockoutTime</th></tr>");
                foreach (var status in groupUserStatuses)
                {
                    var statusClass = status.LockedOut ? "LockedStatus" : "OkStatus";
                    sb.AppendLine($"<tr><td>{status.DisplayName}</td><td class='{statusClass}'>{status.LockedOut}</td><td>{status.LastLogon}</td><td>{status.AccountLockoutTime}</td></tr>");
                    
                }
                sb.AppendLine("</table>");
            }

            // var bb = new StringBuilder();
            sb.AppendLine( "<h2>System Account Logged In Status</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>ComputerName</th><th>UserName</th><th>SessionID</th><th>SessionState</th><th>IdleTime</th><th>LogonTime</th></tr>");
            
            foreach (var status in serverStatuses)
            {
                
                foreach (var user in status.LoggedInUsers)
                {
                    //sb.AppendLine($"{user.UserName} (Session: {user.SessionName}, State: {user.State}, Idle: {user.IdleTime}, Logon: {user.LogonTime})<br>");
                    sb.AppendLine( $"<tr><td>{status.Server} </td><td> {user.UserName}</td><td> {user.SessionName}</td><td> {user.State}</td><td> {user.IdleTime}</td><td> {user.LogonTime}</td></tr>");
                }
            }
            sb.AppendLine( "</table></body></html>");

            return Content(sb.ToString(), "text/html");
        }
    }
}