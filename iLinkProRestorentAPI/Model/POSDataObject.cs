namespace iLinkProRestorentAPI.Model
{
    public class CategoryDTO
    {
        public int Cat_ID { get; set; } 
        public string? CategoryName { get; set; }
        public int Position { get; set; }
        public int DishCount { get; set; }
    }

    public class ProductDTO
    {
        public string ProductName { get; set; }
        public int? BackColor { get; set; }
        public string? ImageURL { get; set; }
        public int DishID { get; set; }
        public string? ButtonColor { get; set; }
        public byte[]? SaveImageDB { get; set; }
        public decimal DineinRate { get; set; }
    }

    public class TableMaster 
    {
        public string Floor { get; set; }
        public List<Table> tables { get; set; }
    }
    public class Table 
    {
        public string TableName { get; set; }
        public int TableCapacityCount { get; set; }
        public bool IsOccupied { get; set; }
    }
}
