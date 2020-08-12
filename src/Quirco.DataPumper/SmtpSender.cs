using Common.Logging;
using Microsoft.Practices.Unity;
using Quirco.DataPumper.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Quirco.DataPumper
{
    class SmtpSender
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataPumperService));
        private readonly DataPumperConfiguration _configuration;

        public SmtpSender()
        {
            _configuration = new DataPumperConfiguration();
        }

        public void SendEmailAsync(IEnumerable<JobLog> jobLogs)
        {
            if (jobLogs.Count() == 0) return;
            
            int errors = 0;
            SmtpClient smtp = new SmtpClient(_configuration.ServerAdress, _configuration.ServerPort);
            smtp.Credentials = new NetworkCredential(_configuration.EmailFrom, _configuration.PasswordFrom);
            smtp.EnableSsl = true;

            MailMessage message = new MailMessage(_configuration.EmailFrom, _configuration.Targets.First());
            foreach (var target in _configuration.Targets)
            {
                if (target != _configuration.Targets.First()) message.CC.Add(target);
            }

            message.IsBodyHtml = true;
            message.Subject = "Job Errors";
            message.Body = @"<h2>Jobs Errors</h2>";

            foreach (var jobLog in jobLogs)
            {
                message.Body += $@"
                <p>
                    <b>Time:</b> {jobLog.StartDate} - {jobLog.EndDate}
                </p>";

                message.Body += $@"
                <p>
                    <b>Status:</b> {jobLog.Status}
                </p>";

                if (jobLog.Status == SyncStatus.Error)
                message.Body += $@"
                <p>
                    <b>Exception:</b> {jobLog.Message}
                </p>";
                 
                message.Body += "</br>";
            }

            try
            {
                smtp.Send(message);
                Log.Info($"{jobLogs.Count()} reports sent to:");
                foreach (var mes in message.CC) Log.Info(mes.Address);
            }
            catch (Exception e)
            {
                Log.Error($"SmtpSender exception: {e.Message}");
            }
             
        }
    }
}
