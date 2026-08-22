SELECT
    cc.name AS NombreConstraint,
    cc.definition AS Definicion
FROM sys.check_constraints cc
WHERE cc.name = 'CK_Usuarios_Rol';