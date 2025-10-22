using iLinkProRestorentAPI.Model;
using iLinkProRestorentAPI.Model.Custom.Login;

namespace iLinkProRestorentAPI.Interfaces.DBRepository
{
    public interface ILoginRepository
    {
        Task<Tuple<int, string , LoginCredentials>> SignIn(string PIN);
        Task<Tuple<int, string, string>> NewRegistration(string userId);
        Task<Tuple<int, string>> ConfirmRegistration(string userId, string newPassword);
    }
}
