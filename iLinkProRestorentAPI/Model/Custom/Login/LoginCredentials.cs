namespace iLinkProRestorentAPI.Model.Custom.Login
{
    public class LoginCredentials
    {
        public string UserId { get; set; }
        public int CompanyId { get; set; }
        public string UserEmail { get; set; }  = string.Empty;
        public string CompanyName { get; set;} = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string? AppToken {  get; set; } = string.Empty;
        public List<Location>? locations { get; set; }
    }
}
