namespace iLinkProRestorentAPI.DTO
{
    public class ChangePinCode
    {
        public string userId { get; set; }  
        public string CurrentPin { get; set; }
        public string ChangePin  { get; set; }    
        public string ConfirmPin { get; set; }    
    }
}
