using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Dtos;
using SistemaClimatico.Models;
using SistemaClimatico.Services;

namespace SistemaClimatico.Controllers;

// Comunidades monitoreadas. Consultar: cualquier usuario autenticado.
// Crear / editar / eliminar: solo Administrador.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComunidadesController : ControllerBase
{
    private readonly SistemaClimaticoContext _db;
    private readonly BitacoraService _bitacora;
    private readonly NotificadorTiempoReal _notificador;

    public ComunidadesController(SistemaClimaticoContext db, BitacoraService bitacora, NotificadorTiempoReal notificador)
    {
        _db = db;
        _bitacora = bitacora;
        _notificador = notificador;
    }

    // GET api/comunidades
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ComunidadDto>>> GetComunidades()
    {
        var comunidades = await _db.Comunidades.OrderBy(c => c.Nombre).ToListAsync();
        var resultado = new List<ComunidadDto>();

        foreach (var c in comunidades)
        {
            var cantidad = await _db.Sensores.CountAsync(s => s.IdComunidad == c.IdComunidad);
            resultado.Add(Mapear(c, cantidad));
        }

        return Ok(resultado);
    }

    // POST api/comunidades
    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<ActionResult<ComunidadDto>> Crear([FromBody] GuardarComunidadDto dto)
    {
        var error = Validar(dto);
        if (error is not null)
            return BadRequest(new { mensaje = error });

        var comunidad = new Comunidad
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            Latitud = dto.Latitud,
            Longitud = dto.Longitud,
            Activa = true,
            FechaCreacion = DateTime.Now
        };

        _db.Comunidades.Add(comunidad);
        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("COMUNIDAD_CREADA",
            $"Se creó la comunidad '{comunidad.Nombre}' (id {comunidad.IdComunidad}).");
        await _notificador.SensoresActualizadosAsync();

        return CreatedAtAction(nameof(GetComunidades), new { id = comunidad.IdComunidad }, Mapear(comunidad, 0));
    }

    // PUT api/comunidades/3
    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] GuardarComunidadDto dto)
    {
        var comunidad = await _db.Comunidades.FindAsync(id);
        if (comunidad is null)
            return NotFound(new { mensaje = $"No existe una comunidad con id {id}." });

        var error = Validar(dto);
        if (error is not null)
            return BadRequest(new { mensaje = error });

        comunidad.Nombre = dto.Nombre.Trim();
        comunidad.Descripcion = dto.Descripcion?.Trim();
        comunidad.Latitud = dto.Latitud;
        comunidad.Longitud = dto.Longitud;
        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("COMUNIDAD_ACTUALIZADA",
            $"Se editó la comunidad '{comunidad.Nombre}' (id {comunidad.IdComunidad}).");
        await _notificador.SensoresActualizadosAsync();

        return NoContent();
    }

    // DELETE api/comunidades/3  (solo si no tiene sensores asignados)
    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        var comunidad = await _db.Comunidades.FindAsync(id);
        if (comunidad is null)
            return NotFound(new { mensaje = $"No existe una comunidad con id {id}." });

        if (await _db.Sensores.AnyAsync(s => s.IdComunidad == id))
            return Conflict(new { mensaje = "La comunidad tiene sensores asignados. Muévelos a otra comunidad antes de eliminarla." });

        var nombre = comunidad.Nombre;
        _db.Comunidades.Remove(comunidad);
        await _db.SaveChangesAsync();

        await _bitacora.RegistrarAsync("COMUNIDAD_ELIMINADA", $"Se eliminó la comunidad '{nombre}' (id {id}).");
        await _notificador.SensoresActualizadosAsync();

        return NoContent();
    }

    private static string? Validar(GuardarComunidadDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return "El nombre de la comunidad es obligatorio.";
        if (dto.Latitud is < -90 or > 90)
            return "La latitud debe estar entre -90 y 90.";
        if (dto.Longitud is < -180 or > 180)
            return "La longitud debe estar entre -180 y 180.";
        return null;
    }

    private static ComunidadDto Mapear(Comunidad c, int cantidadSensores) => new()
    {
        IdComunidad = c.IdComunidad,
        Nombre = c.Nombre,
        Descripcion = c.Descripcion,
        Latitud = c.Latitud,
        Longitud = c.Longitud,
        Activa = c.Activa,
        CantidadSensores = cantidadSensores
    };
}
