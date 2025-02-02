// Services/ServiceResponse.cs
using Microsoft.EntityFrameworkCore;
using Auth.Models;
using PaypalApi.Context;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Auth.Services
{

public class ServiceResponse<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
}

// Services/AuthService.cs
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<ServiceResponse<AuthResponseDTO>> Login(LoginDTO request)
    {
        var response = new ServiceResponse<AuthResponseDTO>();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
        {
            response.Success = false;
            response.Message = "Usuario no encontrado.";
            return response;
        }

        if (!VerifyPasswordHash(request.Password, user.Password))
        {
            response.Success = false;
            response.Message = "Contraseña incorrecta.";
            return response;
        }

        response.Data = new AuthResponseDTO
        {
            Token = CreateToken(user),
            UserId = user.Id,
            Email = user.Email
        };

        return response;
    }

    public async Task<ServiceResponse<int>> Register(RegisterDTO request)
    {
        var response = new ServiceResponse<int>();

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            response.Success = false;
            response.Message = "Usuario ya existe.";
            return response;
        }

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new ApplicationUser
        {
            Username = request.Username,
            Email = request.Email,
            Password = passwordHash
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        response.Data = user.Id;
        return response;
    }

    private string CreateToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration.GetSection("AppSettings:Token").Value!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }

    private bool VerifyPasswordHash(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
 }