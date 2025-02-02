// Models/ApplicationUser.cs

namespace Auth.Models
{
public class ApplicationUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public required string Email { get; set; }
    public required string Password { get; set; }  
}
// DTOs/LoginDTO.cs
public class LoginDTO
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}

// DTOs/RegisterDTO.cs
public class RegisterDTO
{
    public string Username { get; set; }= string.Empty;
    public required string Email { get; set; }
    public required string Password { get; set; }
}

// Crear una nueva clase llamada AuthResponseDTO
public class AuthResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}
}
