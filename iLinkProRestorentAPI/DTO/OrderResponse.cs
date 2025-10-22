namespace iLinkProRestorentAPI.DTO
{
    public class OrderResponse
    {
        public string OrderID { get; set; }
        public string? TableNo { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime OrderTime { get; set; } 
    }
}
