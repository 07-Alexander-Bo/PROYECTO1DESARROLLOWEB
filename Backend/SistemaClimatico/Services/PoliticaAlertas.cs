namespace SistemaClimatico.Services;

// Reglas para NO generar alertas duplicadas.
//
// Sin esto, un sensor que se queda varios minutos en "Amarillo" generaría una
// alerta nueva con cada lectura (cada 5 segundos). La política tiene dos reglas:
//
//  1) Solo se alerta cuando la situación EMPEORA respecto a la lectura anterior
//     del mismo sensor (Verde -> Amarillo, Amarillo -> Naranja, etc.).
//     Mientras siga igual o mejore, es el mismo episodio y no se vuelve a alertar.
//
//  2) Anti-rebote: si ya hubo una alerta igual (mismo sensor, fenómeno y nivel)
//     dentro de los últimos N minutos (Alertas:CooldownMinutos), tampoco se
//     alerta. Evita el parpadeo cuando el valor oscila justo en un umbral.
public static class PoliticaAlertas
{
    public static int Rango(string nivel) => nivel switch
    {
        "Amarillo" => 1,
        "Naranja" => 2,
        "Rojo" => 3,
        _ => 0 // Verde
    };

    public static bool Empeora(string nivelAnterior, string nivelActual)
        => Rango(nivelActual) > Rango(nivelAnterior);

    public static bool DebeGenerarAlerta(string nivelAnterior, string nivelActual, bool existeAlertaIgualReciente)
        => Empeora(nivelAnterior, nivelActual) && !existeAlertaIgualReciente;
}
