namespace iLinkProRestorentAPI.DTO
{
    public class ChangePassword
    {
        public string? UserId { get; set; }
        public string? OTPCode { get; set; }    
        public string? Password { get; set; } 
        public string? ConfirmPassword { get; set;}     
    }
}
