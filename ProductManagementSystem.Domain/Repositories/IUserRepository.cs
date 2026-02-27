using ProductManagementSystem.Domain.Entities;

namespace ProductManagementSystem.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> ObterPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> ObterPorUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<User> CriarAsync(User user, CancellationToken cancellationToken = default);
}
