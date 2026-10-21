using System.Security.Claims;
using SistemaClimatico.Models;

namespace SistemaClimatico.Services;

// Registra en la tabla BitacoraAcciones lo que hacen los usuarios en el sistema
// (login, crear sensores, atender alertas, etc.).
//
// Si no se indica el usuario, se toma automáticamente del token JWT de la
// petición actual. En el login todavía no hay token, por eso ahí se pasa el id.
public class BitacoraService
{
    private readonly SistemaClimaticoContext _db;
    private readonly IHttpContextAccessor _http;

    public BitacoraService(SistemaClimaticoContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task RegistrarAsync(string accion, string? descripcion = null, int? idUsuario = null)
    {
        _db.BitacoraAcciones.Add(new BitacoraAccione
        {
            IdUsuario = idUsuario ?? ObtenerIdUsuarioActual(),
            Accion = Truncar(accion, 100)!,
            Descripcion = Truncar(descripcion, 500),
            FechaHora = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }

    private int? ObtenerIdUsuarioActual()
    {
        var claim = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var id) ? id : null;
    }

    // Las columnas tienen largo máximo (100 y 500): se recorta para no romper el INSERT.
    private static string? Truncar(string? texto, int max)
    {
        if (texto is null) return null;
        return texto.Length <= max ? texto : texto[..max];
    }
}
