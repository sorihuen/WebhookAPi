using Microsoft.AspNetCore.Mvc;
using Auth.Models;
using Auth.Services;

namespace Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // Endpoint para el login
        [HttpPost("login")]
        public async Task<ActionResult<ServiceResponse<string>>> Login(LoginDTO request)
        {
            var response = await _authService.Login(request);

            if (!response.Success)
            {
                return BadRequest(response);  // Si no es exitoso, devuelve un 400 con la respuesta de error.
            }

            return Ok(response);  // Si es exitoso, devuelve un 200 con el token.
        }

        // Endpoint para el registro
        [HttpPost("register")]
        public async Task<ActionResult<ServiceResponse<int>>> Register(RegisterDTO request)
        {
            var response = await _authService.Register(request);

            if (!response.Success)
            {
                return BadRequest(response);  // Si no es exitoso, devuelve un 400 con el mensaje de error.
            }

            return Ok(response);  // Si es exitoso, devuelve un 200 con el id del usuario.
        }
    }
}
