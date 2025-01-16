using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Net.Sockets;
using System.IO;
using System.Text.Json;

namespace ggy_app_service_account_report.Services
{
    public class AccountStatusService
    {
        public List<UserStatus> GetADUserStatus(List<string> users, string domain)
        {
            var userStatuses = new List<UserStatus>();
            using (var context = new PrincipalContext(ContextType.Domain, domain))
            {
                foreach (var user in users)
                {
                    var userPrincipal = UserPrincipal.FindByIdentity(context, user);
                    if (userPrincipal != null)
                    {
                        userStatuses.Add(new UserStatus
                        {
                            DisplayName = userPrincipal.DisplayName,
                            LockedOut = userPrincipal.IsAccountLockedOut(),
                            LastLogon = userPrincipal.LastLogon,
                            AccountLockoutTime = userPrincipal.AccountLockoutTime,

                        });
                    }
                }
            }
            return userStatuses;
        }

        public bool IsPortOpen(string server, int port)
        {
            try
            {
                using (var client = new TcpClient(server, port))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    public class UserStatus
    {
        public string DisplayName { get; set; }
        public bool LockedOut { get; set; }
        public DateTime? LastLogon { get; set; }
        public DateTime? AccountLockoutTime { get; set;}
    }

    public class SessionInfo
    {
        public string UserName { get; set; }
        public string SessionName { get; set; }
        public DateTime LogonTime { get; set; }
        public string State { get; set; }
        public string IdleTime { get; set; }
    }
    public class Configuration
{
    public Dictionary<string, List<string>> UserGroups { get; set; }
    public List<string> Servers { get; set; }
}
    public class ConfigurationService
{
    private readonly string _configFilePath;

    public ConfigurationService(string configFilePath)
    {
        _configFilePath = configFilePath;
    }

    public Configuration GetConfiguration()
    {
        var jsonString = File.ReadAllText(_configFilePath);
        return JsonSerializer.Deserialize<Configuration>(jsonString);
    }
}
}