namespace SistemaClimatico.Services;

// Resultado de evaluar una lectura contra los umbrales de riesgo
public record ResultadoEvaluacion(string Nivel, string? TipoFenomeno, string Mensaje);

// Motor que decide, a partir del tipo de sensor y el valor leído,
// qué nivel de alerta corresponde (Verde/Amarillo/Naranja/Rojo)
// y qué fenómeno se está detectando.
// Los umbrales son simulados para efectos del proyecto de curso.
public static class MotorAlertas
{
    public static ResultadoEvaluacion Evaluar(string tipoSensor, decimal valor)
    {
        return tipoSensor switch
        {
            "Temperatura" => EvaluarTemperatura(valor),
            "Humedad" => EvaluarHumedad(valor),
            "Viento" => EvaluarViento(valor),
            "Lluvia" => EvaluarLluvia(valor),
            "NivelRio" => EvaluarNivelRio(valor),
            _ => new ResultadoEvaluacion("Verde", null, "Sin umbrales definidos para este tipo de sensor.")
        };
    }

    private static ResultadoEvaluacion EvaluarTemperatura(decimal valor)
    {
        // Riesgo de helada (frío extremo)
        if (valor <= 0)
            return new ResultadoEvaluacion("Rojo", "Helada",
                $"Temperatura de {valor}°C: riesgo de helada severa.");
        if (valor <= 3)
            return new ResultadoEvaluacion("Naranja", "Helada",
                $"Temperatura de {valor}°C: riesgo de helada.");

        // Riesgo de calor extremo / incendio forestal
        if (valor >= 45)
            return new ResultadoEvaluacion("Rojo", "IncendioForestal",
                $"Temperatura de {valor}°C: calor extremo, alto riesgo de incendio forestal.");
        if (valor >= 40)
            return new ResultadoEvaluacion("Naranja", "IncendioForestal",
                $"Temperatura de {valor}°C: calor elevado, riesgo de incendio forestal.");
        if (valor >= 35)
            return new ResultadoEvaluacion("Amarillo", "IncendioForestal",
                $"Temperatura de {valor}°C: por encima de lo normal.");

        return new ResultadoEvaluacion("Verde", null, $"Temperatura de {valor}°C dentro de rango normal.");
    }

    private static ResultadoEvaluacion EvaluarHumedad(decimal valor)
    {
        // Humedad baja sostenida es indicador de sequía
        if (valor < 10)
            return new ResultadoEvaluacion("Rojo", "Sequia",
                $"Humedad relativa de {valor}%: condición de sequía severa.");
        if (valor < 20)
            return new ResultadoEvaluacion("Naranja", "Sequia",
                $"Humedad relativa de {valor}%: riesgo de sequía.");
        if (valor < 30)
            return new ResultadoEvaluacion("Amarillo", "Sequia",
                $"Humedad relativa de {valor}%: por debajo de lo normal.");

        return new ResultadoEvaluacion("Verde", null, $"Humedad relativa de {valor}% dentro de rango normal.");
    }

    private static ResultadoEvaluacion EvaluarViento(decimal valor)
    {
        if (valor >= 90)
            return new ResultadoEvaluacion("Rojo", "Tormenta",
                $"Viento de {valor} km/h: tormenta severa.");
        if (valor >= 60)
            return new ResultadoEvaluacion("Naranja", "Tormenta",
                $"Viento de {valor} km/h: riesgo de tormenta.");
        if (valor >= 40)
            return new ResultadoEvaluacion("Amarillo", "Tormenta",
                $"Viento de {valor} km/h: por encima de lo normal.");

        return new ResultadoEvaluacion("Verde", null, $"Viento de {valor} km/h dentro de rango normal.");
    }

    private static ResultadoEvaluacion EvaluarLluvia(decimal valor)
    {
        if (valor >= 100)
            return new ResultadoEvaluacion("Rojo", "Inundacion",
                $"Lluvia de {valor} mm: alto riesgo de inundación.");
        if (valor >= 50)
            return new ResultadoEvaluacion("Naranja", "Tormenta",
                $"Lluvia de {valor} mm: riesgo de tormenta/inundación.");
        if (valor >= 20)
            return new ResultadoEvaluacion("Amarillo", "Tormenta",
                $"Lluvia de {valor} mm: por encima de lo normal.");

        return new ResultadoEvaluacion("Verde", null, $"Lluvia de {valor} mm dentro de rango normal.");
    }

    private static ResultadoEvaluacion EvaluarNivelRio(decimal valor)
    {
        if (valor >= 4)
            return new ResultadoEvaluacion("Rojo", "Inundacion",
                $"Nivel de río de {valor} m: inundación en curso o inminente.");
        if (valor >= 3)
            return new ResultadoEvaluacion("Naranja", "Inundacion",
                $"Nivel de río de {valor} m: riesgo alto de inundación.");
        if (valor >= 2)
            return new ResultadoEvaluacion("Amarillo", "Inundacion",
                $"Nivel de río de {valor} m: por encima de lo normal.");

        return new ResultadoEvaluacion("Verde", null, $"Nivel de río de {valor} m dentro de rango normal.");
    }
}
