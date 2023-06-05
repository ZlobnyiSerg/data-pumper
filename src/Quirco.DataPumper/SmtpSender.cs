using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using Common.Logging;
using Quirco.DataPumper.DataModels;

namespace Quirco.DataPumper
{
    internal class SmtpSender : ILogsSender
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DataPumperService));
        private readonly DataPumperConfiguration _configuration;

        public SmtpSender(DataPumperConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Send(ICollection<JobLog> jobLogs)
        {
            if (!jobLogs.Any(l=>l.Status == SyncStatus.Error))
                return;
            
            if (jobLogs.Count == 0 || string.IsNullOrEmpty(_configuration.Recipients)) 
                return;

            var smtp = new SmtpClient(_configuration.ServerAdress, _configuration.ServerPort)
            {
                Credentials = new NetworkCredential(_configuration.EmailFrom, _configuration.PasswordFrom),
                EnableSsl = true
            };

            var message = new MailMessage(_configuration.EmailFrom, _configuration.Recipients)
            {
                IsBodyHtml = true,
                Subject = "Job Errors",
                Body = @"<h2>Jobs Errors</h2>"
            };

            var body = new StringBuilder();
            foreach (var jobLog in jobLogs.Where(j => j.Status == SyncStatus.Error))
            {
                body.Append($@"
<p>
    <h5>{jobLog.TableSync.TableName}</h5>
    <ul>
        <li><b>Processed / deleted:</b> {jobLog.RecordsProcessed} / {jobLog.RecordsDeleted}</li>
        <li><b>Time:</b> {jobLog.StartDate} - {jobLog.EndDate}</li>
        <li><b>Status:</b> {jobLog.Status}</li>
        <li><b>Exception:</b> {jobLog.Message}</li>
    </ul>
</p>
 <hr/>");
            }

            message.Body += body;

            try
            {
                smtp.Send(message);
                Log.Info($"{jobLogs.Count} reports sent to: {_configuration.Recipients}");
            }
            catch (Exception e)
            {
                Log.Error($"SmtpSender exception: {e.Message}");
            }
        }
    }
}