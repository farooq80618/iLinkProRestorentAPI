using Dapper;
using iLinkProRestorentAPI.ApplicationSettings.Email;
using iLinkProRestorentAPI.Context;
using iLinkProRestorentAPI.Enums;
using iLinkProRestorentAPI.Interfaces.DBRepository;
using iLinkProRestorentAPI.Model;
using iLinkProRestorentAPI.Model.Custom.Login;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace iLinkProRestorentAPI.Repositories
{
    public class LoginRepository : ILoginRepository
    {
        private readonly DapperContext _context;
        private readonly IConfiguration _configuration;
        private readonly GenerateEmail _generateEmail;

        public LoginRepository(DapperContext context, IConfiguration configuration, GenerateEmail generateEmail)
        {
            _generateEmail = generateEmail;
            _context = context;
            _configuration = configuration;
        }

        public async Task<Tuple<int, string, LoginCredentials>> SignIn(string userPIN)
        {
            #region Script Executor

            //ScriptExecutorRepository scriptExecutorRepository = new ScriptExecutorRepository(_context);
            //try
            //{
            //    await scriptExecutorRepository.CheckMaxScriptNoAndAutomaticallyExecuteScriptIfNeeded();
            //}
            //catch
            //{

            //}
            //finally
            //{ 
            //    scriptExecutorRepository = null;    
            //}

            #endregion

            try
            {
                userPIN = CryptoHelper.Encrypt(userPIN);
                var registrationQuery = @"
                 SELECT UserId, EmailID , UserType 
                 FROM Registration 
                 WHERE Rtrim(Password) = @Password And Rtrim(UserType) IN ('APP User' , 'Super Admin')";

                using (var connection = _context.CreateConnection())
                {
                    var registrationParameters = new { Password = userPIN };
                    var registrationResult = await connection.QueryFirstOrDefaultAsync<dynamic>(registrationQuery, registrationParameters);

                    if (registrationResult == null)
                    {
                        return Tuple.Create<int, string, LoginCredentials>((int)ApplicationEnum.APIStatus.Failed, "Invalid credentials", null);
                    }

                    string userIdFromRegistration = registrationResult.UserId;
                    string userEmailFromRegistration = registrationResult.EmailID;
                    string userRight = registrationResult.UserType;

                    var companyQuery = @"
                    SELECT Top 1 Id , HotelName 
                    FROM Hotel ";

                    var companyResult = await connection.QueryFirstOrDefaultAsync<dynamic>(companyQuery);

                    if (companyResult == null)
                    {
                        return Tuple.Create<int, string, LoginCredentials>((int)ApplicationEnum.APIStatus.Failed, "Company not found", null);
                    }

                    var companyId = companyResult.Id;
                    string companyName = companyResult.HotelName;

                    var loginCredentials = new LoginCredentials
                    {
                        UserId = userIdFromRegistration.TrimEnd(),
                        UserEmail = userEmailFromRegistration.TrimEnd(),
                        CompanyId = Convert.ToInt32(companyId),
                        CompanyName = companyName.TrimEnd(),
                        UserRole = userRight,
                    };

                    var token = GenerateJwtToken(loginCredentials);
                    loginCredentials.AppToken = token;  
                    return Tuple.Create<int, string, LoginCredentials>((int)ApplicationEnum.APIStatus.Success, token, loginCredentials);
                }
            }
            catch (Exception ex)
            {
                // Return failure with the exception message
                return Tuple.Create<int, string, LoginCredentials>((int)ApplicationEnum.APIStatus.Failed, ex.Message, null);
            }
        }

        public async Task<Tuple<int, string, string>> NewRegistration(string userId)
        {
            try
            {
                var registrationQuery = @"
                 SELECT UserId, EmailID , IsNull(isLoginPortal , 0) isLoginPortal
                    FROM Registration 
                    WHERE (rtrim(UserId) = @UserId or EmailID = @UserId ) 
                    and UserType = 'Admin'  
                    ";

                using (var connection = _context.CreateConnection())
                {
                    var registrationParameters = new { UserId = userId };
                    var registrationResult = await connection.QueryFirstOrDefaultAsync<dynamic>(registrationQuery, registrationParameters);

                    if (registrationResult == null)
                    {
                        return Tuple.Create<int, string, string>((int)ApplicationEnum.APIStatus.Failed, "Invalid credentials", null);
                    }

                    string userIdFromRegistration = registrationResult.UserId;
                    string userEmailFromRegistration = registrationResult.EmailID;
                    bool isLoginPortal = registrationResult.isLoginPortal ?? false;

                    if (isLoginPortal == true)
                        return Tuple.Create<int, string, string>((int)ApplicationEnum.APIStatus.Success, "Already registered.", null);

                    if(Convert.ToString(userEmailFromRegistration) == "")
                        return Tuple.Create<int, string, string>((int)ApplicationEnum.APIStatus.Failed, "Email is not found.", null);
                    int randomNumber = GenerateRandomSixDigitNumber();
                    bool emailFlag = _generateEmail.SendEmail(userEmailFromRegistration , userIdFromRegistration, randomNumber.ToString(), "iLink Professionals. Inc");
                    if (emailFlag)
                    {
                        var query = @"
                        UPDATE Registration 
                        SET 
                            OTPCode = @OTPCode
                        WHERE 
                           rtrim(UserId) = @UserId";

                        var parameters = new
                        {
                            OTPCode = randomNumber,
                            UserId = userIdFromRegistration,
                        };
                        var rowsAffected = await connection.ExecuteAsync(query, parameters);
                        if (rowsAffected == 0)
                            return Tuple.Create<int, string, string>((int)ApplicationEnum.APIStatus.Failed, "Something went wrong.", null);
                    }
                    else
                    {
                        return Tuple.Create<int, string, string>((int)ApplicationEnum.APIStatus.Failed, "Unable to sent email.Please contact to vendor.", null);
                    }
                    
                    return Tuple.Create<int, string, string>((int)ApplicationEnum.APIStatus.Success, "Emailed successfully.", randomNumber.ToString());
                }
            }
            catch (Exception ex)
            {
                return Tuple.Create<int, string, string>((int)ApplicationEnum.APIStatus.Failed, ex.Message, null);
            }
        }

        public async Task<Tuple<int, string>> ConfirmRegistration(string userId , string OTPCode , string newPassword , string ConfirmPassword)
        {
            try 
            {
                var registrationQuery = @"
                 SELECT UserID , OTPCode
                    FROM Registration 
                    WHERE (rtrim(UserId) = @UserId or EmailID = @UserId ) 
                    and UserType = 'Admin'";

                using (var connection = _context.CreateConnection())
                {
                    var registrationParameters = new { UserId = userId };
                    var registrationResult = await connection.QueryFirstOrDefaultAsync<dynamic>(registrationQuery, registrationParameters);

                    if (registrationResult == null)
                    {
                        return Tuple.Create<int, string>((int)ApplicationEnum.APIStatus.Failed, "Invalid credentials");
                    }

                    string UserID = registrationResult.UserID;  
                    string sysOTPCode = registrationResult.OTPCode;

                    if (sysOTPCode != OTPCode)
                        return Tuple.Create<int, string>((int)ApplicationEnum.APIStatus.Failed, "Invalid OTP.");
                    
                    if(newPassword != ConfirmPassword)
                        return Tuple.Create<int, string>((int)ApplicationEnum.APIStatus.Failed, "Password and confirm password mismatch.");

                    var query = @"
                        UPDATE Registration 
                        SET 
                            PortalPassword = @PortalPassword ,
                            PortalLastLogin = getDate() ,
                            IsLoginPortal = 1 
                        WHERE 
                           rtrim(UserId) = @UserId";

                    var parameters = new
                    {
                        PortalPassword = newPassword,
                        UserId = UserID,
                    };
                    var rowsAffected = await connection.ExecuteAsync(query, parameters);
                    if (rowsAffected == 0)
                        return Tuple.Create<int, string>((int)ApplicationEnum.APIStatus.Failed, "Something went wrong.");
                }

                return Tuple.Create<int, string>((int)ApplicationEnum.APIStatus.Success, "Password changed successfully.");
            }
            catch(Exception ex)  
            {
                return Tuple.Create<int, string>((int)ApplicationEnum.APIStatus.Failed, "Invalid credentials");
            }
        }

        public static int GenerateRandomSixDigitNumber()
        {
            Random random = new Random();
            return random.Next(100000, 999999); // Generates a number between 100000 and 999999
        }

        private string GenerateJwtToken(LoginCredentials credentials)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentialsJwt = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, credentials.UserId.ToString()),
                new Claim(ClaimTypes.Email, credentials.UserEmail),
                new Claim("CompanyId", credentials.CompanyId.ToString()),
                new Claim(ClaimTypes.Role, credentials.UserRole)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.MaxValue,//DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"])),
                signingCredentials: credentialsJwt
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}