using Microsoft.Extensions.Configuration;
using System;

namespace iLinkProRestorentAPI.ApplicationSettings.Email
{
    public class EmailTemplate
    {
        private readonly IConfiguration _configuration;

        // Inject IConfiguration into the constructor
        public EmailTemplate(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateEmailTemplate(string UserName, string OTPCode, string CompanyName)
        {
            // Access AppLink from appsettings.json
            string appLink = _configuration["AppLink"];

            string strTemplate = @"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>iDigiPro Code</title>
                    <style>
                        body {
                            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            background-color: #f4f4f4;
                            margin: 0;
                            padding: 0;
                        }
                        .container {
                            width: 100%;
                            max-width: 600px;
                            margin: auto;
                            background: #ffffff;
                            padding: 30px;
                            border-radius: 10px;
                            box-shadow: 0 10px 20px rgba(0, 0, 0, 0.1);
                            animation: fadeIn 1s ease-out;
                        }
                        .header {
                            background-color: #ff6f00;
                            color: white;
                            padding: 15px 0;
                            text-align: center;
                            border-radius: 10px 10px 0 0;
                            margin-bottom: 20px;
                        }
                        .content {
                            padding: 20px;
                            font-size: 16px;
                            color: #333;
                        }
                        .otp {
                            font-size: 36px;
                            font-weight: bold;
                            color: #ff6f00;
                            background: #333;
                            border: 2px solid #ff6f00;
                            padding: 15px;
                            border-radius: 8px;
                            text-align: center;
                            margin: 20px 0;
                            display: inline-block;
                            width: 100%;
                        }
                        .footer {
                            text-align: center;
                            padding: 15px;
                            font-size: 14px;
                            color: #777;
                            margin-top: 30px;
                            border-top: 1px solid #f0f0f0;
                        }
                        .footer a {
                            color: #ff6f00;
                            text-decoration: none;
                        }
                        @keyframes fadeIn {
                            from { opacity: 0; }
                            to { opacity: 1; }
                        }
                        .button {
                            display: inline-block;
                            background-color: #ff6f00;
                            color: white;
                            padding: 12px 20px;
                            text-decoration: none;
                            border-radius: 5px;
                            text-align: center;
                            font-weight: bold;
                            margin-top: 20px;
                            border: none;
                        }
                        .button:hover {
                            background-color: #e65c00;
                        }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>iDigiPro Portal Verification Code</h2>
                        </div>
                        <div class='content'>
                <h3>Hi " + UserName + @",</h3>
                            <p>Thank you for using our service! Your code is:</p>
                            <div class='otp'>" + OTPCode + @"</div>
                            <p>This code is valid for 60 minutes. Please enter it into the application to proceed.</p>
                            <p>If you did not request this code, please ignore this message.</p>
                            <a href='" + appLink + @"' class='button'>Go to Application</a>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2025 " + CompanyName + @". All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            return strTemplate;
        }
    }
}