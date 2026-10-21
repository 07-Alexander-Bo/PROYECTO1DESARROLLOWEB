using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Dtos;
using SistemaClimatico.Utilidades;

namespace SistemaClimatico.Controllers;

// Solo lectura: cualquier usuario autenticado.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HistorialController : ControllerBase
{
    private readonly Models.SistemaClimaticoContext _db;

    public HistorialController(Models.SistemaClimaticoContext db)
    {
        _db = db;
    }

    // GET api/historial?tipoFenomeno=Inundacion&desde=2026-08-01&hasta=2026-08-22
    // El historial se llena automáticamente cada vez que se genera una alerta
    // de nivel Amarillo, Naranja o Rojo (ver AlertasController / SensoresController.CrearLectura).
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HistorialEventoDto>>> GetHistorial(
        [FromQuery] string? tipoFenomeno,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta)
    {
        var query = _db.HistorialEventos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(tipoFenomeno))
            query = query.Where(h => h.TipoFenomeno == tipoFenomeno);

        // Las fechas se guardan en UTC: los filtros se convierten antes de consultar.
        var desdeUtc = desde.HasValue ? FechaUtil.AUtc(desde.Value) : default;
        var hastaUtc = hasta.HasValue ? FechaUtil.HastaInclusivo(hasta.Value) : default;

        if (desde.HasValue)
            query = query.Where(h => h.FechaHora >= desdeUtc);

        if (hasta.HasValue)
            query = query.Where(h => h.FechaHora <= hastaUtc);

        var historial = await query
            .OrderByDescending(h => h.FechaHora)
            .Select(h => new HistorialEventoDto
            {
                IdEvento = h.IdEvento,
                IdAlerta = h.IdAlerta,
                TipoFenomeno = h.TipoFenomeno,
                Descripcion = h.Descripcion,
                FechaHora = h.FechaHora
            })
            .ToListAsync();

        return Ok(historial);
    }

    // GET api/historial/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<HistorialEventoDto>> GetEvento(long id)
    {
        var evento = await _db.HistorialEventos.FindAsync(id);

        if (evento is null)
            return NotFound(new { mensaje = $"No existe un evento con id {id}." });

        return Ok(new HistorialEventoDto
        {
            IdEvento = evento.IdEvento,
            IdAlerta = evento.IdAlerta,
            TipoFenomeno = evento.TipoFenomeno,
            Descripcion = evento.Descripcion,
            FechaHora = evento.FechaHora
        });
    }
}
