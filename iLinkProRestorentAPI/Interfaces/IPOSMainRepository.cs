using iLinkProRestorentAPI.DTO;
using iLinkProRestorentAPI.Model;

namespace iLinkProRestorentAPI.Interfaces
{
    public interface IPOSMainRepository
    {
        Task<Tuple<int, string, List<CategoryDTO>>> GetCategoryAsync(string filter);
        Task<Tuple<int, string, List<ProductDTO>>> GetProductAsync(string Category);
        Task<Tuple<int, string, List<TableMaster>>> GetTablesAsync();
        Task<Tuple<int, string, List<Modifiers>>> GetModifireAsync(string Dish);
        Task<Tuple<int, string, List<PizzaSize>>> GetPizzaAsync();
        Task<Tuple<int, string, OrderResponse>> InsertOrderAsync(OrderMaster order);
        Task<Tuple<int, string, OrderResponse>> InsertTakeAwayOrderAsync(OrderMaster order);
        Task<Tuple<int, string, ViewOrder>> ViewOrderAsync(string ticketNo);
    }
}
