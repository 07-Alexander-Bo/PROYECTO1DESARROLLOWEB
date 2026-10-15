using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SistemaClimatico.Services;

namespace SistemaClimatico.Hubs;

// Canal de tiempo real (SignalR). Solo se conectan usuarios con sesión iniciada.
// El servidor empuja datos a los navegadores; los navegadores no envían nada por aquí.
//
// URL: /hubs/monitoreo
[Authorize]
public class MonitoreoHub : Hub
{
    private readonly SimuladorControl _simulador;

    public MonitoreoHub(SimuladorControl simulador)
    {
        _simulador = simulador;
    }

    public override async Task OnConnectedAsync()
    {
        // Apenas se conecta, el navegador sabe si el simulador está activo o en pausa.
        await Clients.Caller.SendAsync(EventosTiempoReal.SimuladorEstado, _simulador.Activo);
        await base.OnConnectedAsync();
    }
}
