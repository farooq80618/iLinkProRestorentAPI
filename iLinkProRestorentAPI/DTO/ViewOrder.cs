namespace iLinkProRestorentAPI.DTO
{
    public class ViewOrder
    {
        public string TicketNo { get; set; }
        public string TableNo { get; set; }
        public string OrderStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderDetails> Details { get; set; }
    }

    public class OrderDetails
    { 
        public string Dish { get; set; }
        public int Quantity { get; set; }
        public string Category { get; set; }
        public string Notes { get; set; }
    }
}
