namespace SistemaClimatico.Hubs;

// Nombres de los mensajes que el servidor envía a los navegadores por SignalR.
// El frontend (core/tiempo-real.ts) escucha exactamente estos nombres.
public static class EventosTiempoReal
{
    // Llega una lectura nueva de un sensor. Payload: EstadoSensorDto
    // (última lectura ya evaluada: valor, nivel de riesgo, mensaje).
    public const string LecturaRecibida = "LecturaRecibida";

    // Se generó una alerta nueva. Payload: AlertaDto
    public const string AlertaNueva = "AlertaNueva";

    // Alguien marcó una alerta como atendida. Payload: id de la alerta (long)
    public const string AlertaAtendida = "AlertaAtendida";

    // Se creó, editó, activó o desactivó un sensor (sin payload):
    // las pantallas vuelven a pedir la lista de sensores.
    public const string SensoresActualizados = "SensoresActualizados";

    // El simulador se pausó o reanudó. Payload: bool (true = activo)
    public const string SimuladorEstado = "SimuladorEstado";

    // Un administrador reinició el monitoreo (se borraron lecturas, alertas e historial).
    // Sin payload: las pantallas vuelven a pedir sus datos.
    public const string MonitoreoReiniciado = "MonitoreoReiniciado";
}
