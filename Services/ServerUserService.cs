using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AccountStatusClass.Services
{
    public class ServerUserInfo
    {
        public string ComputerName { get; set; }
        public string UserName { get; set; }
        public string SessionName { get; set; }
        public string Id { get; set; }
        public string State { get; set; }
        public string IdleTime { get; set; }
        public DateTime LogonTime { get; set; }
    }

    public class ServerUserService
    {
        public List<ServerUserInfo> GetLoggedInUsers(string server, List<string> interestedUsers)
        {
            var users = new List<ServerUserInfo>();
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "quser",
                        Arguments = $"/server:{server}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                var regex = new Regex(@"\s+");

                foreach (var line in lines.Skip(1)) // Skip the header line
                {
                    var trimmedLine = Regex.Replace(line.Trim(), @"\s+", " ");
                    var parts = trimmedLine.Split(' ');

                    if (parts.Length >= 7)
                    {
                        string userName = parts[0];
                        //Console.WriteLine($"Username: {userName}");
                        //Console.WriteLine($"Interested Users: {string.Join(", ", interestedUsers)}");
                        if (interestedUsers.Any(u => u.Equals(userName, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (parts[3] == "Active")
                            {
                                users.Add(new ServerUserInfo
                                {
                                    ComputerName = server,
                                    UserName = userName,
                                    SessionName = parts[1],
                                    Id = parts[2],
                                    State = parts[3],
                                    IdleTime = parts[4],
                                    LogonTime = DateTime.Parse($"{parts[5]} {parts[6]} {parts[7]}")
                                });
                            }
                            else
                            {
                                users.Add(new ServerUserInfo
                                {
                                    ComputerName = server,
                                    UserName = userName,
                                    SessionName = null,
                                    Id = parts[1],
                                    State = "Disconnected",
                                    IdleTime = parts[3],
                                    LogonTime = DateTime.Parse($"{parts[4]} {parts[5]} {parts[6]}")
                                });
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Insufficient parts in line: {line}"); // Debugging statement
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception while getting logged in users for server {server}: {ex.Message}");
            }

            return users;
        }
    }
}