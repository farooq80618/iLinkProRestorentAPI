namespace iLinkProRestorentAPI.Model.Custom.Login
{
    public class Location
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int DefaultLocation { get; set; }
        public string CompanyName { get;set; } = string.Empty;
    }
}
