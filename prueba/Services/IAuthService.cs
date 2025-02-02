// Services/IAuthService.cs

using Auth.Models;

namespace Auth.Services
{

public interface IAuthService
{
    Task<ServiceResponse<AuthResponseDTO>> Login(LoginDTO request);
    Task<ServiceResponse<int>> Register(RegisterDTO request);
}
}