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
        public List<JobLog> JobLogs { get; set; }
        private readonly DataPumperConfiguration _configuration;
        public SmtpSender()
        {
            _configuration = new DataPumperConfiguration();
            JobLogs = new List<JobLog>();
        }

        public async Task SendEmailAsync()
        {
            SmtpClient smtp = new SmtpClient(_configuration.ServerAdress, _configuration.ServerPort);
            smtp.Credentials = new NetworkCredential(_configuration.EmailFrom, _configuration.PasswordFrom);
            smtp.EnableSsl = true;

            MailMessage message = new MailMessage(_configuration.EmailFrom,_configuration.Targets.First());
            foreach(var i in _configuration.Targets)
            {
                if(i != _configuration.Targets.First()) message.CC.Add(i);
            }


            message.Subject = "Jobs Errors";

            message.Body = @"<h2>Jobs Errors</h2>";

            foreach(var i in JobLogs)
            {
                message.Body += $@"
                <p>
                    <b>Time:</b> {i.StartDate} - {i.EndDate}
                </p>";

                message.Body += $@"
                <p>
                    <b>Exception:</b> {i.Message}
                </p>";

            }

            message.IsBodyHtml = true;

            
            await smtp.SendMailAsync(message);
        }
    }
}
