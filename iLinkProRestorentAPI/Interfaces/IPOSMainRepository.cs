using iLinkProRestorentAPI.Model;

namespace iLinkProRestorentAPI.Interfaces
{
    public interface IPOSMainRepository
    {
        Task<Tuple<int, string, List<CategoryDTO>>> GetCategoryAsync(string filter);
        Task<Tuple<int, string, List<ProductDTO>>> GetProductAsync(string Category);
        Task<Tuple<int, string, List<TableMaster>>> GetTablesAsync();
    }
}
