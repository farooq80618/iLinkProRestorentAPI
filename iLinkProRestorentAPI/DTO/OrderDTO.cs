namespace iLinkProRestorentAPI.DTO
{

    public enum OrderType : int 
    {
        DineIn = 1,
        TakeAway = 2,
    }
    public class OrderMaster
    {
        public string? Table { get; set; }
        public OrderType orderType { get; set; }    
        public string? Operator { get; set; }
        public decimal TotalAmount { get; set; } 
        public decimal SubTotal { get;set; }
        public string? PaymentMode { get;set; }  
        public string? Notes { get; set;  }
        public int? NoOfPerson { get; set; }
        public List<OrderDetail> orderDetails { get; set; } 
    }

    public class OrderDetail
    {
        public string Dish { get ; set; }
        public decimal Rate { get ; set; }
        public int Quantity { get ; set; }
        public decimal Amount { get ; set; }
        public decimal VATPer { get; set; }
        public decimal VATAmount { get; set; }
        public decimal STPer { get; set; }
        public decimal STAmount { get; set; }
        public decimal SCPer { get; set; }
        public decimal SCAmount { get; set; }
        public decimal DiscountPer { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Notes { get; set; }
        public string? Category { get; set; }
        public bool? isComboDeal { get; set; }   
    }
}
 