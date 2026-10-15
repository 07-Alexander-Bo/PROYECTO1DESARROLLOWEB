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
}

export interface CrearSensorDto {
  nombre: string;
  tipo: TipoSensor;
  unidadMedida: string;
  ubicacion?: string | null;
}

export interface ActualizarSensorDto {
  nombre: string;
  tipo: TipoSensor;
  unidadMedida: string;
  ubicacion?: string | null;
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
