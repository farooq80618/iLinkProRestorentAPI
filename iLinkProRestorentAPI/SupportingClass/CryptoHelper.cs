using System;
using System.Text;

public class CryptoHelper
{
    public static string Encrypt(string password)
    {
        byte[] encode = Encoding.UTF8.GetBytes(password);
        string strmsg = Convert.ToBase64String(encode);
        return strmsg;
    }

    public static string Decrypt(string encryptpwd)
    {
        UTF8Encoding encodepwd = new UTF8Encoding();
        Decoder decoder = encodepwd.GetDecoder();

        byte[] todecode_byte = Convert.FromBase64String(encryptpwd);
        int charCount = decoder.GetCharCount(todecode_byte, 0, todecode_byte.Length);
        char[] decoded_char = new char[charCount];
        decoder.GetChars(todecode_byte, 0, todecode_byte.Length, decoded_char, 0);

        string decryptpwd = new string(decoded_char);
        return decryptpwd;
    }
}
