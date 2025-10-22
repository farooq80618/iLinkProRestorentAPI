using iLinkProRestorentAPI.ApplicationSettings.AppSetting;
using iLinkProRestorentAPI.DTO;
using iLinkProRestorentAPI.Enums;
using iLinkProRestorentAPI.Interfaces.DBRepository;
using iLinkProRestorentAPI.Model;
using iLinkProRestorentAPI.Model.Custom.Login;
using Microsoft.AspNetCore.Authorization;
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

        [Authorize]
        [HttpPost("ChangePin")]
        public async Task<IActionResult> ChangePin(ChangePinCode changePinCode)
        {
            if (changePinCode == null)
                return BadRequest(APIResponse<string>.FailResponse("Please provide pins."));

            if (changePinCode.CurrentPin == "")
                return BadRequest(APIResponse<string>.FailResponse("Please provide current pin."));

            if (changePinCode.ChangePin == "")
                return BadRequest(APIResponse<string>.FailResponse("Please provide change pin."));

            if (changePinCode.ChangePin != changePinCode.ConfirmPin)
                return BadRequest(APIResponse<string>.FailResponse("Pin mismatched with change pin."));

            var check = await _repo.ConfirmRegistration(changePinCode.userId, changePinCode.ChangePin);
            var (status, Warningmessage) = check;
            if (status == (int)ApplicationEnum.APIStatus.Failed)
                return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
            return Ok(APIResponse<string>.SuccessResponse(Warningmessage));
        }

        [Authorize]
        [HttpPost("LogOut")]
        public async Task<IActionResult> Logout()
        {
            return Ok(APIResponse<string>.SuccessResponse("Current user is logout successfully."));
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
