using Microsoft.AspNetCore.SignalR;
using SistemaClimatico.Dtos;
using SistemaClimatico.Hubs;

namespace SistemaClimatico.Services;

// Punto único para enviar mensajes en tiempo real a todos los navegadores conectados.
// Si el envío falla, solo se registra en el log: nunca debe romper la operación
// principal (guardar una lectura, atender una alerta, etc.).
public class NotificadorTiempoReal
{
    private readonly IHubContext<MonitoreoHub> _hub;
    private readonly ILogger<NotificadorTiempoReal> _logger;

    public NotificadorTiempoReal(IHubContext<MonitoreoHub> hub, ILogger<NotificadorTiempoReal> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task LecturaAsync(EstadoSensorDto estado)
        => EnviarAsync(EventosTiempoReal.LecturaRecibida, estado);

    public Task AlertaNuevaAsync(AlertaDto alerta)
        => EnviarAsync(EventosTiempoReal.AlertaNueva, alerta);

    public Task AlertaAtendidaAsync(long idAlerta)
        => EnviarAsync(EventosTiempoReal.AlertaAtendida, idAlerta);

    public Task SensoresActualizadosAsync()
        => EnviarAsync(EventosTiempoReal.SensoresActualizados, null);

    public Task SimuladorAsync(bool activo)
        => EnviarAsync(EventosTiempoReal.SimuladorEstado, activo);

    public Task MonitoreoReiniciadoAsync()
        => EnviarAsync(EventosTiempoReal.MonitoreoReiniciado, null);

    private async Task EnviarAsync(string metodo, object? dato)
    {
        try
        {
            if (dato is null)
                await _hub.Clients.All.SendAsync(metodo);
            else
                await _hub.Clients.All.SendAsync(metodo, dato);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo enviar '{Metodo}' por SignalR: {Mensaje}", metodo, ex.Message);
        }
    }
}
