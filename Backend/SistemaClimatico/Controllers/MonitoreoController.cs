using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaClimatico.Models;
using SistemaClimatico.Services;

namespace SistemaClimatico.Controllers;

// Acciones sobre todo el sistema de monitoreo. Solo Administrador.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class MonitoreoController : ControllerBase
{
    private readonly SistemaClimaticoContext _db;
    private readonly SimuladorControl _simulador;
    private readonly NotificadorTiempoReal _notificador;
    private readonly BitacoraService _bitacora;

    public MonitoreoController(
        SistemaClimaticoContext db,
        SimuladorControl simulador,
        NotificadorTiempoReal notificador,
        BitacoraService bitacora)
    {
        _db = db;
        _simulador = simulador;
        _notificador = notificador;
        _bitacora = bitacora;
    }

    // POST api/monitoreo/reiniciar
    // Reinicia el sistema de monitoreo: borra las lecturas, las alertas y el historial
    // de eventos, y el simulador vuelve a empezar desde valores normales.
    // NO se tocan sensores, comunidades, usuarios ni la bitácora (queda constancia del reinicio).
    [HttpPost("reiniciar")]
    public async Task<IActionResult> Reiniciar(CancellationToken ct)
    {
        int lecturas, alertas, eventos;

        // Mientras se borra, el simulador no puede escribir lecturas nuevas.
        await _simulador.Candado.WaitAsync(ct);
        try
        {
            // Orden por las llaves foráneas: historial -> alertas -> lecturas.
            eventos = await _db.HistorialEventos.ExecuteDeleteAsync(ct);
            alertas = await _db.Alertas.ExecuteDeleteAsync(ct);
            lecturas = await _db.LecturasSensores.ExecuteDeleteAsync(ct);

            _simulador.Reiniciar();
        }
        finally
        {
            _simulador.Candado.Release();
        }

        await _bitacora.RegistrarAsync("MONITOREO_REINICIADO",
            $"Se reinició el monitoreo: {lecturas} lecturas, {alertas} alertas y {eventos} eventos eliminados.");
        await _notificador.MonitoreoReiniciadoAsync();

        return Ok(new
        {
            mensaje = "Sistema de monitoreo reiniciado.",
            lecturasEliminadas = lecturas,
            alertasEliminadas = alertas,
            eventosEliminados = eventos
        });
    }
}
