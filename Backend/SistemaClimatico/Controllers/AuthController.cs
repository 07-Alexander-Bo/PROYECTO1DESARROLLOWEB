using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Dtos;
using SistemaClimatico.Models;
using SistemaClimatico.Services;

namespace SistemaClimatico.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly SistemaClimaticoContext _db;
    private readonly TokenService _tokenService;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public AuthController(SistemaClimaticoContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    // Roles permitidos por el CHECK constraint CK_Usuarios_Rol de la base de datos.
    // Si algún día agregas un rol nuevo, tiene que coincidir con este constraint EN LA BD también.
    private static readonly string[] RolesValidos = { "Administrador", "Operador", "Usuario" };

    // POST /api/auth/registro
    // Nota: por ahora este endpoint es público para poder crear el primer usuario.
    // Cuando ya tengas al menos un Administrador, considera protegerlo con
    // [Authorize(Roles = "Administrador")] para que solo un Administrador pueda
    // crear otros usuarios (esto lo dejamos como siguiente paso).
    [HttpPost("registro")]
    public async Task<ActionResult<UsuarioDto>> Registro(RegistroRequest request)
    {
        var rol = string.IsNullOrWhiteSpace(request.Rol) ? "Usuario" : request.Rol;

        if (!RolesValidos.Contains(rol, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                mensaje = $"El rol '{rol}' no es válido.",
                rolesPermitidos = RolesValidos
            });
        }

        var yaExiste = await _db.Usuarios.AnyAsync(u => u.Correo == request.Correo);
        if (yaExiste)
        {
            return Conflict(new { mensaje = "Ya existe un usuario registrado con ese correo." });
        }

        var usuario = new Usuario
        {
            NombreCompleto = request.NombreCompleto,
            Correo = request.Correo,
            Rol = rol,
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        usuario.PasswordHash = _hasher.HashPassword(usuario, request.Password);

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Registro), new { id = usuario.IdUsuario }, MapearDto(usuario));
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Correo == request.Correo);
        if (usuario is null)
        {
            return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
        }

        if (!usuario.Activo)
        {
            return Unauthorized(new { mensaje = "El usuario está desactivado. Contacta al administrador." });
        }

        var resultado = _hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
        if (resultado == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { mensaje = "Correo o contraseña incorrectos." });
        }

        var (token, expira) = _tokenService.GenerarToken(usuario);

        return Ok(new LoginResponse
        {
            Token = token,
            Expira = expira,
            Usuario = MapearDto(usuario)
        });
    }

    // GET /api/auth/perfil  (requiere token válido)
    [Authorize]
    [HttpGet("perfil")]
    public async Task<ActionResult<UsuarioDto>> Perfil()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (idClaim is null || !int.TryParse(idClaim, out var idUsuario))
        {
            return Unauthorized();
        }

        var usuario = await _db.Usuarios.FindAsync(idUsuario);
        if (usuario is null)
        {
            return NotFound();
        }

        return Ok(MapearDto(usuario));
    }

    // GET /api/auth/admin-only  (ejemplo de endpoint restringido por rol)
    [Authorize(Roles = "Administrador")]
    [HttpGet("admin-only")]
    public IActionResult SoloAdmin()
    {
        return Ok(new { mensaje = $"Hola {User.Identity!.Name}, tienes acceso de Administrador." });
    }

    private static UsuarioDto MapearDto(Usuario u) => new()
    {
        IdUsuario = u.IdUsuario,
        NombreCompleto = u.NombreCompleto,
        Correo = u.Correo,
        Rol = u.Rol,
        Activo = u.Activo,
        FechaCreacion = u.FechaCreacion
    };
}
