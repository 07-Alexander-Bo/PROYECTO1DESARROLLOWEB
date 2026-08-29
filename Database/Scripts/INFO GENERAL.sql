USE SistemaClimatico;
GO
SELECT 'Usuarios' AS Tabla, COUNT(*) AS Registros FROM Usuarios
UNION ALL
SELECT 'Sensores', COUNT(*) FROM Sensores
UNION ALL
SELECT 'Lecturas', COUNT(*) FROM LecturasSensores
UNION ALL
SELECT 'Alertas', COUNT(*) FROM Alertas;
GO