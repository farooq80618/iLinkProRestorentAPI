using System.Net;
using System.Net.Mail;

namespace iLinkProRestorentAPI.ApplicationSettings.Email
{
    public class GenerateEmail
    {
        private readonly IConfiguration _configuration;
        private readonly EmailTemplate _emailTemplate;

        public GenerateEmail(IConfiguration configuration, EmailTemplate emailTemplate)
        {
            _configuration = configuration;
            _emailTemplate = emailTemplate;
        }

        public bool SendEmail(string recipientEmail, string userName, string otpCode, string companyName)
        {
            try
            {
                // Get SMTP settings
                var smtpSettings = SmtpConfiguration.GetSmtpSettings(_configuration);

                // Create SMTP client
                SmtpClient smtpClient = new SmtpClient(smtpSettings.Host, smtpSettings.Port)
                {
                    Credentials = new NetworkCredential(smtpSettings.UserName, smtpSettings.Password),
                    EnableSsl = smtpSettings.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                // Generate email body using the template
                string emailBody = _emailTemplate.GenerateEmailTemplate(userName, otpCode, companyName);

                // Create email message
                MailMessage mail = new MailMessage
                {
                    From = new MailAddress(smtpSettings.From, "iDigiPro portal recovery email."),
                    Subject = "Code for iDiGiPro Dashboard.",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                // Add recipient
                mail.To.Add(new MailAddress(recipientEmail));

                // Add CC (optional)
                mail.CC.Add(new MailAddress("ilpdeveloper102@gmail.com"));

                // Send email
                smtpClient.Send(mail);

                return true;
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using a logging framework)
                Console.WriteLine($"Error sending email: {ex.Message}");
                return false;
            }
        }
    }
}
