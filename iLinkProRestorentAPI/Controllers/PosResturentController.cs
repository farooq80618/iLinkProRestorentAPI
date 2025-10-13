using iLinkProRestorentAPI.ApplicationSettings.AppSetting;
using iLinkProRestorentAPI.DTO;
using iLinkProRestorentAPI.Enums;
using iLinkProRestorentAPI.Interfaces;
using iLinkProRestorentAPI.Model;
using iLinkProRestorentAPI.Model.Custom.Login;
using Microsoft.AspNetCore.Mvc;

namespace iLinkProRestorentAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PosResturentController : ControllerBase
    {
        private readonly IPOSMainRepository _repo;
        public PosResturentController(IPOSMainRepository repo)
        {
            _repo = repo;
        }
  
        [HttpGet("GetCategoryAsync")]
        public async Task<IActionResult> GetCategoryAsync()
        {
            var check = await _repo.GetCategoryAsync();
            var (status, Warningmessage , resuli) = check;
            if (status == (int)ApplicationEnum.APIStatus.Failed)
                return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
            return Ok(APIResponse<List<CategoryDTO>>.SuccessResponse(resuli));
        }

        [HttpGet("GetProductAsync")]
        public async Task<IActionResult> GetProductAsync(string CategoryName)
        {
            var check = await _repo.GetProductAsync(CategoryName);
            var (status, Warningmessage, resuli) = check;
            if (status == (int)ApplicationEnum.APIStatus.Failed)
                return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
            return Ok(APIResponse<List<ProductDTO>>.SuccessResponse(resuli));
        }

        [HttpGet("GetTableDetailsAsync")]
        public async Task<IActionResult> GetTableDetailsAsync()
        {
            var check = await _repo.GetTablesAsync();
            var (status, Warningmessage, resuli) = check;
            if (status == (int)ApplicationEnum.APIStatus.Failed)
                return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
            return Ok(APIResponse<List<TableMaster>>.SuccessResponse(resuli));
        }
    }
}
