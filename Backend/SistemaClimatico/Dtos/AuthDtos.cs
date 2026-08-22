using System.ComponentModel.DataAnnotations;

namespace SistemaClimatico.Dtos;

// Lo que manda el cliente para registrarse
public class RegistroRequest
{
    [Required, MaxLength(150)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Correo { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    // Opcional: si no se manda, el usuario queda como "Usuario".
    // Solo un Admin debería poder crear otros Admins (ver nota en el controller).
    public string? Rol { get; set; }
}

// Lo que manda el cliente para loguearse
public class LoginRequest
{
    [Required, EmailAddress]
    public string Correo { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

// Lo que devuelve el login
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expira { get; set; }
    public UsuarioDto Usuario { get; set; } = null!;
}

// Representación segura del usuario (nunca exponer PasswordHash)
public class UsuarioDto
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
