using iLinkProRestorentAPI.ApplicationSettings.AppSetting;
using iLinkProRestorentAPI.DTO;
using iLinkProRestorentAPI.Enums;
using iLinkProRestorentAPI.Interfaces.DBRepository;
using iLinkProRestorentAPI.Model;
using iLinkProRestorentAPI.Model.Custom.Login;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace iLinkProRestorentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ILoginRepository _repo;
        public LoginController(ILoginRepository repo)
        {
            _repo = repo;
        }

        [HttpPost("ChangePasswordViaOTP")]
        public async Task<IActionResult> ChangePasswordViaOTP(ChangePassword changePassword)
        {
            if (changePassword == null)
                return BadRequest(APIResponse<string>.FailResponse("Please provide credentials."));

            if (changePassword.UserId == "")
                return BadRequest(APIResponse<string>.FailResponse("Please provide user id."));

            if (changePassword.OTPCode == "")
                return BadRequest(APIResponse<string>.FailResponse("Please provide OTP code."));

            if (changePassword.Password == "")
                return BadRequest(APIResponse<string>.FailResponse("Please provide password."));

            var check = await _repo.ConfirmRegistration(changePassword.UserId, changePassword.OTPCode, changePassword.Password, changePassword.ConfirmPassword);
            var (status, Warningmessage) = check;
            if (status == (int)ApplicationEnum.APIStatus.Failed)
                return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
            return Ok(APIResponse<string>.SuccessResponse(Warningmessage));
        }

        [HttpPost("SignInUser")]
        public async Task<IActionResult> SignInUser(SignIn pinCredential)
        {
            if(pinCredential == null)
                return BadRequest(APIResponse<string>.FailResponse("Please provide credentials."));

            if(pinCredential.PIN == "")
                return BadRequest(APIResponse<string>.FailResponse("Please provide PIN."));
            var result = await _repo.SignIn(pinCredential.PIN);
            var (statusCode, message, loginCredentials) = result;
            if (statusCode == (int)ApplicationEnum.APIStatus.Success)
            {
                return Ok(APIResponse<LoginCredentials>.SuccessResponse(loginCredentials));
            }
            else 
            {
                return BadRequest(APIResponse<LoginCredentials>.FailResponse(message));
            }
        }
    }
}
