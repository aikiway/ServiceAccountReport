using Microsoft.AspNetCore.Mvc;
using ggy_app_service_account_report.Services;
using AccountStatusClass.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
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
        private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "list.json");

        public ReportController(AccountStatusService accountStatusService, ConfigurationService configurationService, ServerUserService serverUserService)
        {
            _accountStatusService = accountStatusService;
            _configurationService = configurationService;
            _serverUserService = serverUserService;
        }

        [HttpGet("Landing")]
        public IActionResult ReportLanding()
        {
            return Content(System.IO.File.ReadAllText("wwwroot/report-landing.html"), "text/html");
        }

        [HttpGet("GenerateReport")]
        public IActionResult GenerateReport()
        {
            var config = _configurationService.GetConfiguration();
            if (config == null)
            {
                return BadRequest("Failed to load configuration.");
            }
            var userGroups = config.UserGroups;
            var servers = config.Servers;
            var domain = "mfcgd.com";

            var userStatuses = userGroups.SelectMany(group => _accountStatusService.GetADUserStatus(group.Value, domain)).ToList();
            var serverStatuses = servers.Select(server => new
            {
                Server = server,
                IsPortOpen = _accountStatusService.IsPortOpen(server, 3389),
                LoggedInUsers = _serverUserService.GetLoggedInUsers(server, userGroups.SelectMany(g => g.Value).ToList())
            }).ToList();

            var cssStyles = @"
                <style>
                    h1 { font-family: Arial, Helvetica, sans-serif; color: #e68a00; font-size: 20px; }
                    h2 { font-family: Arial, Helvetica, sans-serif; color: #000099; font-size: 16px; }
                    table { font-size: 12px; border: 0px; font-family: Arial, Helvetica, sans-serif; }
                    td { padding: 4px; margin: 0px; border: 0; }
                    th { background: #395870; background: linear-gradient(#49708f, #293f50); color: #fff; font-size: 11px; text-transform: uppercase; padding: 10px 15px; vertical-align: middle; }
                    tbody tr:nth-child(even) { background: #f0f0f2; }
                    #CreationDate { font-family: Arial, Helvetica, sans-serif; color: #ff3300; font-size: 12px; }
                    .LockedStatus { color: #ff0000; font-weight: bold; }
                    .OkStatus { color: #008000; font-weight: bold; }
                </style>";

            var sb = new StringBuilder();
            sb.AppendLine("<html><head>" + cssStyles + "</head><body><h1>GAITS System Account Status Report</h1>");
            foreach (var group in userGroups)
            {
                sb.AppendLine($"<h2>{group.Key} - System Account Status</h2>");
                var groupUserStatuses = _accountStatusService.GetADUserStatus(group.Value, domain);
                sb.AppendLine("<table>");
                sb.AppendLine("<tr><th>DisplayName</th><th>LockedOut</th><th>LastLogonDate</th><th>AccountLockoutTime</th></tr>");
                foreach (var status in groupUserStatuses)
                {
                    var statusClass = status.LockedOut ? "LockedStatus" : "OkStatus";
                    sb.AppendLine($"<tr><td>{status.DisplayName}</td><td class='{statusClass}'>{status.LockedOut.ToString()}</td><td>{status.LastLogon}</td><td>{status.AccountLockoutTime}</td></tr>");
                }
                sb.AppendLine("</table>");
            }

            sb.AppendLine("<h2>System Account Logged In Status</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>ComputerName</th><th>UserName</th><th>SessionID</th><th>SessionState</th><th>IdleTime</th><th>LogonTime</th></tr>");

            foreach (var status in serverStatuses)
            {
                foreach (var user in status.LoggedInUsers)
                {
                    sb.AppendLine($"<tr><td>{status.Server}</td><td>{user.UserName}</td><td>{user.SessionName}</td><td>{user.State}</td><td>{user.IdleTime}</td><td>{user.LogonTime}</td></tr>");
                }
            }
            sb.AppendLine("</table></body></html>");

            return Content(sb.ToString(), "text/html");
        }

        [HttpGet("UpdateUserList")]
        public IActionResult UpdateUserList()
        {
            return Content(System.IO.File.ReadAllText("wwwroot/update-list.html"), "text/html");
        }

        [HttpPost("AddItem")]
        public IActionResult AddItem([FromBody] ListItem item)
        {
            var list = LoadList();
            Console.WriteLine($"Received item: Type={item.Type}, Name={item.Name}, UserGroup={item.UserGroup}");
            if (item.Type == "user")
            {
                if (list.UserGroups.ContainsKey(item.UserGroup))
                {
                    if (!list.UserGroups[item.UserGroup].Contains(item.Name))
                    {
                        list.UserGroups[item.UserGroup].Add(item.Name);
                    }
                }
                else
                {
                    return BadRequest(new { message = "User group does not exist." });
                }
            }
            else if (item.Type == "userGroup")
            {
                if (!list.UserGroups.ContainsKey(item.Name))
                {
                    list.UserGroups[item.Name] = new List<string>();
                }
            }
            else if (item.Type == "server")
            {
                if (!list.Servers.Contains(item.Name))
                {
                    list.Servers.Add(item.Name);
                }
            }
            SaveList(list);
            return Ok(new { message = "Item added successfully." });
        }

        [HttpPost("RemoveItem")]
        public IActionResult RemoveItem([FromBody] ListItem item)
        {
            var list = LoadList();
            if (item.Type == "user")
            {
                // Find the user group for the selected user
                string userGroup = list.UserGroups.FirstOrDefault(ug => ug.Value.Contains(item.Name)).Key;
                if (string.IsNullOrEmpty(userGroup))
                {
                    return BadRequest(new { message = "User not found in any user group." });
                }
                list.UserGroups[userGroup].Remove(item.Name);
            }
            else if (item.Type == "userGroup")
            {
                list.UserGroups.Remove(item.Name);
            }
            else if (item.Type == "server")
            {
                list.Servers.Remove(item.Name);
            }
            SaveList(list);
            return Ok(new { message = "Item removed successfully." });
        }

        [HttpGet("userGroups")]
        public IActionResult GetUserGroups()
        {
            var list = LoadList();
            return Ok(new { items = list.UserGroups.Keys.ToList() });
        }

        [HttpGet("{type}s")]
        public IActionResult GetItems(string type)
        {
            var list = LoadList();
            if (type == "userGroup")
            {
                return Ok(new { items = list.UserGroups.Keys.ToList() });
            }
            else if (type == "server")
            {
                return Ok(new { items = list.Servers });
            }
            else if (type == "user")
            {
               // Collect all unique users from all user groups
               // var allUsers = new HashSet<string>(list.UserGroups.Values.SelectMany(users => users));
                //return Ok(new { items = allUsers.ToList() });
                // Collect all users from all user groups
                var allUsers = list.UserGroups.Values.SelectMany(users => users).ToList();
                return Ok(new { items = allUsers });
            }
            return BadRequest("Invalid type");
        }

        private ListData LoadList()
        {
            if (!System.IO.File.Exists(_filePath))
            {
                return new ListData();
            }
            var json = System.IO.File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<ListData>(json);
        }

        private void SaveList(ListData list)
        {
            var json = JsonConvert.SerializeObject(list, Formatting.Indented);
            System.IO.File.WriteAllText(_filePath, json);
        }
    }

    public class ListItem
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string UserGroup { get; set; } // Added UserGroup property
    }

    public class ListData
    {
        public Dictionary<string, List<string>> UserGroups { get; set; } = new Dictionary<string, List<string>>();
        public List<string> Servers { get; set; } = new List<string>();
    }
}