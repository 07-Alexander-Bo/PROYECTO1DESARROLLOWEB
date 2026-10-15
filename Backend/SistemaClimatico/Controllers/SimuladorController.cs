using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaClimatico.Services;

namespace SistemaClimatico.Controllers;

// Estado y control del simulador de sensores.
// Consultar: cualquier usuario autenticado. Pausar/reanudar: solo Administrador.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SimuladorController : ControllerBase
{
    private readonly SimuladorControl _control;
    private readonly NotificadorTiempoReal _notificador;
    private readonly BitacoraService _bitacora;

    public SimuladorController(SimuladorControl control, NotificadorTiempoReal notificador, BitacoraService bitacora)
    {
        _control = control;
        _notificador = notificador;
        _bitacora = bitacora;
    }

    // GET api/simulador
    [HttpGet]
    public IActionResult GetEstado()
        => Ok(new { activo = _control.Activo, intervaloSegundos = _control.IntervaloSegundos });

    // POST api/simulador/pausar
    [Authorize(Roles = "Administrador")]
    [HttpPost("pausar")]
    public async Task<IActionResult> Pausar()
        => await CambiarAsync(false, "SIMULADOR_PAUSADO", "Se pausó el simulador de sensores.");

    // POST api/simulador/reanudar
    [Authorize(Roles = "Administrador")]
    [HttpPost("reanudar")]
    public async Task<IActionResult> Reanudar()
        => await CambiarAsync(true, "SIMULADOR_REANUDADO", "Se reanudó el simulador de sensores.");

    private async Task<IActionResult> CambiarAsync(bool activo, string accion, string descripcion)
    {
        if (_control.Activo != activo)
        {
            _control.Activo = activo;
            await _bitacora.RegistrarAsync(accion, descripcion);
            await _notificador.SimuladorAsync(activo);
        }

        return Ok(new { activo = _control.Activo, intervaloSegundos = _control.IntervaloSegundos });
    }
}
