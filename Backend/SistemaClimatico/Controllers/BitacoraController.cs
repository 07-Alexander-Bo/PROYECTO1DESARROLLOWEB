using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Dtos;
using SistemaClimatico.Models;

namespace SistemaClimatico.Controllers;

// Consulta de la bitácora de acciones. Solo para Administradores.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class BitacoraController : ControllerBase
{
    private readonly SistemaClimaticoContext _db;

    public BitacoraController(SistemaClimaticoContext db)
    {
        _db = db;
    }

    // GET api/bitacora?idUsuario=1&accion=LOGIN&desde=2026-10-01&hasta=2026-10-31&limite=100
    [HttpGet]
    public async Task<ActionResult<IEnumerable<BitacoraDto>>> GetBitacora(
        [FromQuery] int? idUsuario,
        [FromQuery] string? accion,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        [FromQuery] int limite = 100)
    {
        var query = _db.BitacoraAcciones.AsQueryable();

        if (idUsuario.HasValue)
            query = query.Where(b => b.IdUsuario == idUsuario.Value);

        if (!string.IsNullOrWhiteSpace(accion))
            query = query.Where(b => b.Accion == accion);

        if (desde.HasValue)
            query = query.Where(b => b.FechaHora >= desde.Value);

        if (hasta.HasValue)
            query = query.Where(b => b.FechaHora <= hasta.Value);

        var registros = await query
            .OrderByDescending(b => b.FechaHora)
            .Take(Math.Clamp(limite, 1, 500))
            .Select(b => new BitacoraDto
            {
                IdBitacora = b.IdBitacora,
                IdUsuario = b.IdUsuario,
                NombreUsuario = b.IdUsuarioNavigation != null ? b.IdUsuarioNavigation.NombreCompleto : null,
                Accion = b.Accion,
                Descripcion = b.Descripcion,
                FechaHora = b.FechaHora
            })
            .ToListAsync();

        return Ok(registros);
    }
}
