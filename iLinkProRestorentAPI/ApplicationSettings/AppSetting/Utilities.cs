namespace iLinkProRestorentAPI.ApplicationSettings.AppSetting
{
    public static class Utilities
    {
        public static string EncryptPassword(string password)
        {
            byte[] encode = System.Text.Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(encode);
        }
        public static string DecryptPassword(string encodedPassword)
        {
            byte[] decode = Convert.FromBase64String(encodedPassword);
            return System.Text.Encoding.UTF8.GetString(decode);
        }
    }
}
