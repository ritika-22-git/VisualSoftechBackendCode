using VisualSoftechBackend.Models;

namespace VisualSoftechBackend.DAO
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<int> CreateAsync(User user);
    }
}
