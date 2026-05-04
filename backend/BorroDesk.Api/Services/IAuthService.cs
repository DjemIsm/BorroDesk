using BorroDesk.Api.DTOs.Auth;

namespace BorroDesk.Api.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
}
