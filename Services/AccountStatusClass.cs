using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Net.Sockets;

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
                            LastLogon = userPrincipal.LastLogon
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

        public List<SessionInfo> GetLoggedInUsers(string server)
        {
            var sessions = new List<SessionInfo>();
            // Implement logic to get logged-in users, e.g., using WMI or other methods
            return sessions;
        }
    }

    public class UserStatus
    {
        public string DisplayName { get; set; }
        public bool LockedOut { get; set; }
        public DateTime? LastLogon { get; set; }
    }

    public class SessionInfo
    {
        public string UserName { get; set; }
        public string SessionName { get; set; }
        public DateTime LogonTime { get; set; }
    }
}