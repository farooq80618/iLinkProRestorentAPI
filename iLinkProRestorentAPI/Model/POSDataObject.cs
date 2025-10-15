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
        //public byte[]? SaveImageDB { get; set; }
        public decimal DineinRate { get; set; }
        public bool ModifierFlag { get; set; }
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

    public class Modifiers
    { 
        public int MIM_ID { get; set; }
        public string ModifierName { get; set; }
        public string Item { get; set; }
        public string Rate { get; set; }
        public int BackColor { get; set; }
    }

    public class PizzaMaster
    {
        public int Pizza_ID { get; set; }
        public string? PizzaName { get; set; }
        public string? PizzaSize { get; set; }
        public string? Desription { get; set; }
        public decimal Rate { get; set; }
        public int ToppingsLimit { get; set; }
        public decimal Discount { get; set; }
        public List<PizzaModifier>? Modifier { get; set; }
    }

    public class PizzaSize
    {
        public int SizeID { get; set; }
        public string? Size { get; set; }
        public List<PizzaMaster>? PizzaMaster { get; set; }
        public List<PizzaTopping>? PizzaTopping { get; set; }
    }
    public class PizzaModifier
    {
        public int PM_ID { get; set; }
        public int PizzaID { get; set; }
        public string? ModifierName { get; set; }
        public decimal Rate { get; set; }
    }
    public class PizzaTopping
    {
        public int T_ID { get; set; }
        public string? ToppingName { get; set; }
        public string? ToppingSize { get; set; }
        public string? PizzaSize { get; set; }
        public decimal Rate { get; set; }
    }
}
