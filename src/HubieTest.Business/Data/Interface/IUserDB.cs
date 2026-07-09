using HubieTest.Dal;

namespace HubieTest.Business.Data
{
    public interface IUserDB
    {
        APP_USER getByLogin(string login);
        APP_USER getById(long userId);
    }
}
