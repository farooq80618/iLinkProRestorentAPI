using Microsoft.Extensions.Configuration;
namespace iLinkProRestorentAPI.ApplicationSettings.Email
{
    public class SmtpConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool EnableSsl { get; set; }
        public string From { get; set; }

        public static SmtpConfiguration GetSmtpSettings(IConfiguration configuration)
        {
            return configuration.GetSection("SmtpSettings").Get<SmtpConfiguration>();
        }
    }
}
