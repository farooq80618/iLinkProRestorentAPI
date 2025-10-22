using iLinkProRestorentAPI.ApplicationSettings.AppSetting;
using iLinkProRestorentAPI.DTO;
using iLinkProRestorentAPI.Enums;
using iLinkProRestorentAPI.Interfaces;
using iLinkProRestorentAPI.Model;
using iLinkProRestorentAPI.Model.Custom.Login;
using Microsoft.AspNetCore.Authorization;
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

        //[Authorize]
        [HttpGet("GetCategoryAsync")]
        public async Task<IActionResult> GetCategoryAsync(string? filter)
        {
            var check = await _repo.GetCategoryAsync(filter);
            var (status, Warningmessage , resuli) = check;
            if (status == (int)ApplicationEnum.APIStatus.Failed)
                return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
            return Ok(APIResponse<List<CategoryDTO>>.SuccessResponse(resuli));
        }

        //[Authorize]
        [HttpGet("GetProductAsync")]
        public async Task<IActionResult> GetProductAsync(string CategoryName)
        {
            var check = await _repo.GetProductAsync(CategoryName);
            var (status, Warningmessage, resuli) = check;
            if (status == (int)ApplicationEnum.APIStatus.Failed)
                return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
            return Ok(APIResponse<List<ProductDTO>>.SuccessResponse(resuli));
        }

        //[Authorize]
        [HttpGet("GetTableDetailsAsync")]
        public async Task<IActionResult> GetTableDetailsAsync()
        {
            var check = await _repo.GetTablesAsync();
            var (status, Warningmessage, resuli) = check;
            if (status == (int)ApplicationEnum.APIStatus.Failed)
                return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
            return Ok(APIResponse<List<TableMaster>>.SuccessResponse(resuli));
        }

        //[Authorize]
        [HttpGet("GetModifierAsync")]
        public async Task<IActionResult> GetModifierAsync(string dishName)
        {
            var check = await _repo.GetModifireAsync(dishName);
            var (status, Warningmessage, resuli) = check;
            if (status == (int)ApplicationEnum.APIStatus.Failed)
                return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
            return Ok(APIResponse<List<Modifiers>>.SuccessResponse(resuli));
        }

        //[Authorize]
        [HttpGet("GetPizzaMasterAsync")]
        public async Task<IActionResult> GetPizzaMaster()
        {
            var check = await _repo.GetPizzaAsync();
            var (status, Warningmessage, resuli) = check;
            if (status == (int)ApplicationEnum.APIStatus.Failed)
                return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
            return Ok(APIResponse<List<PizzaSize>>.SuccessResponse(resuli));
        }

        //[Authorize]
        [HttpPost("GenerateOrderAsync")]
        public async Task<IActionResult> GenerateOrder(OrderMaster order)
        {
            if(order == null)
                return BadRequest(APIResponse<string>.FailResponse("Data is not found."));
            if (order.orderType == OrderType.DineIn)
            {
                var check = await _repo.InsertOrderAsync(order);
                var (status, Warningmessage, resuli) = check;
                if (status == (int)ApplicationEnum.APIStatus.Failed)
                    return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
                return Ok(APIResponse<OrderResponse>.SuccessResponse(resuli , Warningmessage));
            }
            else
            {
                var check = await _repo.InsertTakeAwayOrderAsync(order);
                var (status, Warningmessage, resuli) = check;
                if (status == (int)ApplicationEnum.APIStatus.Failed)
                    return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
                return Ok(APIResponse<OrderResponse>.SuccessResponse(resuli, Warningmessage));
            }
        }

        //[Authorize]
        [HttpGet("ViewOrderDetailAsync")]
        public async Task<IActionResult> ViewOrderDetailAsync(string ticketBNo)
        {
            var check = await _repo.ViewOrderAsync(ticketBNo);
            var (status, Warningmessage, resuli) = check;
            if (status == (int)ApplicationEnum.APIStatus.Failed)
                return BadRequest(APIResponse<string>.FailResponse(Warningmessage));
            return Ok(APIResponse<ViewOrder>.SuccessResponse(resuli));
        }
    }
}
