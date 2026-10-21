// Modelos calcados exactamente de tus DTOs reales (Dtos/*.cs).
// Los nombres de propiedad estan en camelCase porque asi los serializa
// System.Text.Json por defecto en ASP.NET Core.

export type Rol = 'Administrador' | 'Operador' | 'Usuario';
export type NivelAlerta = 'Verde' | 'Amarillo' | 'Naranja' | 'Rojo';

// Segun Dtos/SensorDtos.cs -> TiposSensor.Valores
export const TIPOS_SENSOR = ['Temperatura', 'Humedad', 'Viento', 'Lluvia', 'NivelRio', 'Otro'] as const;
export type TipoSensor = typeof TIPOS_SENSOR[number];

// ---------- Auth (Dtos/AuthDtos.cs) ----------
export interface UsuarioDto {
  idUsuario: number;
  nombreCompleto: string;
  correo: string;
  rol: Rol;
  activo: boolean;
  fechaCreacion: string;
}

export interface LoginResponse {
  token: string;
  expira: string;
  usuario: UsuarioDto;
}

// ---------- Sensores (Dtos/SensorDtos.cs) ----------
export interface SensorDto {
  idSensor: number;
  nombre: string;
  tipo: TipoSensor;
  unidadMedida: string;
  ubicacion?: string | null;
  activo: boolean;
  fechaCreacion: string;
  idComunidad?: number | null;
  nombreComunidad?: string | null;
  latitud?: number | null;
  longitud?: number | null;
}

// Crear y editar un sensor usan los mismos campos.
export interface CrearSensorDto {
  nombre: string;
  tipo: TipoSensor;
  unidadMedida: string;
  ubicacion?: string | null;
  idComunidad?: number | null;
  latitud?: number | null;
  longitud?: number | null;
}

export type ActualizarSensorDto = CrearSensorDto;

// ---------- Comunidades (Dtos/ComunidadDtos.cs) ----------
export interface ComunidadDto {
  idComunidad: number;
  nombre: string;
  descripcion?: string | null;
  latitud: number;
  longitud: number;
  activa: boolean;
  cantidadSensores: number;
}

export interface GuardarComunidadDto {
  nombre: string;
  descripcion?: string | null;
  latitud: number;
  longitud: number;
}

// ---------- Series para los graficos (GET /api/lecturas/series) ----------
export interface PuntoLecturaDto {
  fechaHora: string;
  valor: number;
}

export interface SerieSensorDto {
  idSensor: number;
  nombre: string;
  tipo: TipoSensor;
  unidadMedida: string;
  idComunidad?: number | null;
  puntos: PuntoLecturaDto[];
}

// ---------- Bitacora (Dtos/BitacoraDtos.cs) ----------
export interface BitacoraDto {
  idBitacora: number;
  idUsuario?: number | null;
  nombreUsuario?: string | null;
  accion: string;
  descripcion?: string | null;
  fechaHora: string;
}

export interface UsuarioFiltroDto {
  idUsuario: number;
  nombreCompleto: string;
}

export interface CrearLecturaDto {
  valor: number;
}

export interface LecturaDto {
  idLectura: number;
  idSensor: number;
  valor: number;
  fechaHora: string;
}

// ---------- Alertas (Dtos/AlertaDtos.cs) ----------
export interface AlertaDto {
  idAlerta: number;
  idSensor?: number | null;
  nombreSensor?: string | null;
  nivel: NivelAlerta;
  tipoFenomeno: string;
  mensaje: string;
  valorDetectado?: number | null;
  fechaHora: string;
  atendida: boolean;
}

// GET /api/alertas/estado-actual -> esto es lo que arma el dashboard/semaforo,
// incluye Verde para cada sensor activo aunque no tenga alerta real.
export interface EstadoSensorDto {
  idSensor: number;
  nombreSensor: string;
  tipoSensor: TipoSensor;
  unidadMedida: string;
  ultimoValor?: number | null;
  fechaUltimaLectura?: string | null;
  nivel: NivelAlerta;
  tipoFenomeno?: string | null;
  mensaje: string;
  idComunidad?: number | null;
  latitud?: number | null;
  longitud?: number | null;
}

// ---------- Historial (Dtos/HistorialEventoDtos.cs) ----------
export interface HistorialEventoDto {
  idEvento: number;
  idAlerta?: number | null;
  tipoFenomeno: string;
  descripcion: string;
  fechaHora: string;
}

// ---------- Presentacion de niveles y fenomenos ----------
// Nombres que pide el enunciado: Verde (Normal), Amarillo (Precaución),
// Naranja (Alerta), Rojo (Emergencia).
export const ETIQUETA_NIVEL: Record<NivelAlerta, string> = {
  Verde: 'Normal',
  Amarillo: 'Precaución',
  Naranja: 'Alerta',
  Rojo: 'Emergencia',
};

export const COLOR_NIVEL: Record<NivelAlerta, string> = {
  Verde: 'var(--sc-verde)',
  Amarillo: 'var(--sc-amarillo)',
  Naranja: 'var(--sc-naranja)',
  Rojo: 'var(--sc-rojo)',
};

const NOMBRE_FENOMENO: Record<string, string> = {
  Inundacion: 'Inundación',
  Sequia: 'Sequía',
  Tormenta: 'Tormenta',
  Helada: 'Helada',
  IncendioForestal: 'Incendio forestal',
  Otro: 'Otro',
};

// 'IncendioForestal' -> 'Incendio forestal', 'Inundacion' -> 'Inundación', etc.
export function nombreFenomeno(tipo?: string | null): string {
  if (!tipo) return '';
  return NOMBRE_FENOMENO[tipo] ?? tipo;
}

// Umbrales de riesgo por tipo de sensor, calcados de Services/MotorAlertas.cs.
// Solo sirven para dibujar las lineas de referencia en los graficos: la decision
// de generar una alerta la toma siempre el backend.
export interface UmbralGrafico {
  valor: number;
  nivel: NivelAlerta;
}

export const UMBRALES_GRAFICO: Record<string, UmbralGrafico[]> = {
  Temperatura: [
    { valor: 0, nivel: 'Rojo' }, { valor: 3, nivel: 'Naranja' },
    { valor: 35, nivel: 'Amarillo' }, { valor: 40, nivel: 'Naranja' }, { valor: 45, nivel: 'Rojo' },
  ],
  Humedad: [
    { valor: 10, nivel: 'Rojo' }, { valor: 20, nivel: 'Naranja' }, { valor: 30, nivel: 'Amarillo' },
  ],
  Viento: [
    { valor: 40, nivel: 'Amarillo' }, { valor: 60, nivel: 'Naranja' }, { valor: 90, nivel: 'Rojo' },
  ],
  Lluvia: [
    { valor: 20, nivel: 'Amarillo' }, { valor: 50, nivel: 'Naranja' }, { valor: 100, nivel: 'Rojo' },
  ],
  NivelRio: [
    { valor: 2, nivel: 'Amarillo' }, { valor: 3, nivel: 'Naranja' }, { valor: 4, nivel: 'Rojo' },
  ],
};

export const ETIQUETA_TIPO_SENSOR: Record<string, string> = {
  Temperatura: 'Temperatura',
  Humedad: 'Humedad relativa',
  Viento: 'Viento',
  Lluvia: 'Lluvia',
  NivelRio: 'Nivel del río',
  Otro: 'Otro',
};
