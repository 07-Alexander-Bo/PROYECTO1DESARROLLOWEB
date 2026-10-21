import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable, Subscription } from 'rxjs';
import { SensoresService } from '../../core/sensores';
import { ComunidadesService } from '../../core/comunidades';
import { MonitoreoService, ReinicioResultado } from '../../core/monitoreo';
import { TiempoRealService } from '../../core/tiempo-real';
import { ComunidadDto, SensorDto, TIPOS_SENSOR, TipoSensor } from '../../core/models';

type Pestana = 'sensores' | 'comunidades' | 'sistema';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './admin.html',
  styleUrl: './admin.css',
})
export class Admin implements OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private srv = inject(SensoresService);
  private comunidadesSrv = inject(ComunidadesService);
  private monitoreoSrv = inject(MonitoreoService);
  private tiempoReal = inject(TiempoRealService);
  private subs = new Subscription();

  tipos = TIPOS_SENSOR;
  pestana = signal<Pestana>('sensores');

  sensores = signal<SensorDto[]>([]);
  comunidades = signal<ComunidadDto[]>([]);

  // ---------- Sensores (crear y editar comparten formulario) ----------
  formSensorAbierto = signal(false);
  sensorEditandoId = signal<number | null>(null); // null = se esta creando uno nuevo
  guardandoSensor = signal(false);
  errorSensor = signal<string | null>(null);

  formSensor = this.fb.group({
    nombre: ['', Validators.required],
    tipo: ['Temperatura' as TipoSensor, Validators.required],
    unidadMedida: ['', Validators.required],
    ubicacion: [''],
    idComunidad: [null as number | null],
    latitud: [null as number | null],
    longitud: [null as number | null],
  });

  // ---------- Comunidades ----------
  formComunidadAbierto = signal(false);
  comunidadEditandoId = signal<number | null>(null);
  guardandoComunidad = signal(false);
  errorComunidad = signal<string | null>(null);
  comunidadAEliminar = signal<number | null>(null);

  formComunidad = this.fb.group({
    nombre: ['', Validators.required],
    descripcion: [''],
    latitud: [null as number | null, [Validators.required, Validators.min(-90), Validators.max(90)]],
    longitud: [null as number | null, [Validators.required, Validators.min(-180), Validators.max(180)]],
  });

  // ---------- Reinicio del monitoreo ----------
  confirmandoReinicio = signal(false);
  reiniciando = signal(false);
  resultadoReinicio = signal<ReinicioResultado | null>(null);
  errorReinicio = signal<string | null>(null);

  ngOnInit() {
    this.cargar();
    // Si otro administrador cambia sensores o comunidades, la lista se actualiza sola.
    this.subs.add(this.tiempoReal.sensoresActualizados$.subscribe(() => this.cargar()));
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
  }

  cargar() {
    this.srv.listar(true).subscribe({ next: (data) => this.sensores.set(data) });
    this.comunidadesSrv.listar().subscribe({ next: (data) => this.comunidades.set(data) });
  }

  // =============== Sensores ===============
  nuevoSensor() {
    if (this.formSensorAbierto() && this.sensorEditandoId() === null) {
      this.formSensorAbierto.set(false);
      return;
    }
    this.sensorEditandoId.set(null);
    this.errorSensor.set(null);
    this.formSensor.reset({
      nombre: '', tipo: 'Temperatura', unidadMedida: '', ubicacion: '', idComunidad: null, latitud: null, longitud: null,
    });
    this.formSensorAbierto.set(true);
  }

  editarSensor(s: SensorDto) {
    this.sensorEditandoId.set(s.idSensor);
    this.errorSensor.set(null);
    this.formSensor.reset({
      nombre: s.nombre,
      tipo: s.tipo,
      unidadMedida: s.unidadMedida,
      ubicacion: s.ubicacion ?? '',
      idComunidad: s.idComunidad ?? null,
      latitud: s.latitud ?? null,
      longitud: s.longitud ?? null,
    });
    this.formSensorAbierto.set(true);
    this.pestana.set('sensores');
  }

  cancelarSensor() {
    this.formSensorAbierto.set(false);
    this.sensorEditandoId.set(null);
  }

  guardarSensor() {
    if (this.formSensor.invalid) {
      this.formSensor.markAllAsTouched();
      return;
    }
    const v = this.formSensor.getRawValue();
    const dto = {
      nombre: v.nombre ?? '',
      tipo: (v.tipo ?? 'Temperatura') as TipoSensor,
      unidadMedida: v.unidadMedida ?? '',
      ubicacion: v.ubicacion || null,
      idComunidad: v.idComunidad ? Number(v.idComunidad) : null,
      latitud: v.latitud ?? null,
      longitud: v.longitud ?? null,
    };

    this.guardandoSensor.set(true);
    this.errorSensor.set(null);
    const id = this.sensorEditandoId();
    const peticion: Observable<unknown> = id === null ? this.srv.crear(dto) : this.srv.actualizar(id, dto);

    peticion.subscribe({
      next: () => {
        this.guardandoSensor.set(false);
        this.cancelarSensor();
        this.cargar();
      },
      error: (err) => {
        this.guardandoSensor.set(false);
        this.errorSensor.set(err?.error?.mensaje ?? 'No se pudo guardar el sensor.');
      },
    });
  }

  activarDesactivar(s: SensorDto) {
    const accion = s.activo ? this.srv.desactivar(s.idSensor) : this.srv.activar(s.idSensor);
    accion.subscribe({ next: () => this.cargar() });
  }

  // =============== Comunidades ===============
  nuevaComunidad() {
    if (this.formComunidadAbierto() && this.comunidadEditandoId() === null) {
      this.formComunidadAbierto.set(false);
      return;
    }
    this.comunidadEditandoId.set(null);
    this.errorComunidad.set(null);
    this.formComunidad.reset({ nombre: '', descripcion: '', latitud: null, longitud: null });
    this.formComunidadAbierto.set(true);
  }

  editarComunidad(c: ComunidadDto) {
    this.comunidadEditandoId.set(c.idComunidad);
    this.errorComunidad.set(null);
    this.formComunidad.reset({
      nombre: c.nombre, descripcion: c.descripcion ?? '', latitud: Number(c.latitud), longitud: Number(c.longitud),
    });
    this.formComunidadAbierto.set(true);
  }

  cancelarComunidad() {
    this.formComunidadAbierto.set(false);
    this.comunidadEditandoId.set(null);
  }

  guardarComunidad() {
    if (this.formComunidad.invalid) {
      this.formComunidad.markAllAsTouched();
      return;
    }
    const v = this.formComunidad.getRawValue();
    const dto = {
      nombre: v.nombre ?? '',
      descripcion: v.descripcion || null,
      latitud: Number(v.latitud),
      longitud: Number(v.longitud),
    };

    this.guardandoComunidad.set(true);
    this.errorComunidad.set(null);
    const id = this.comunidadEditandoId();
    const peticion: Observable<unknown> = id === null ? this.comunidadesSrv.crear(dto) : this.comunidadesSrv.actualizar(id, dto);

    peticion.subscribe({
      next: () => {
        this.guardandoComunidad.set(false);
        this.cancelarComunidad();
        this.cargar();
      },
      error: (err) => {
        this.guardandoComunidad.set(false);
        this.errorComunidad.set(err?.error?.mensaje ?? 'No se pudo guardar la comunidad.');
      },
    });
  }

  eliminarComunidad(c: ComunidadDto) {
    this.errorComunidad.set(null);
    this.comunidadesSrv.eliminar(c.idComunidad).subscribe({
      next: () => { this.comunidadAEliminar.set(null); this.cargar(); },
      error: (err) => {
        this.comunidadAEliminar.set(null);
        this.errorComunidad.set(err?.error?.mensaje ?? 'No se pudo eliminar la comunidad.');
      },
    });
  }

  // =============== Reiniciar el monitoreo ===============
  reiniciar() {
    this.reiniciando.set(true);
    this.errorReinicio.set(null);
    this.resultadoReinicio.set(null);
    this.monitoreoSrv.reiniciar().subscribe({
      next: (r) => {
        this.reiniciando.set(false);
        this.confirmandoReinicio.set(false);
        this.resultadoReinicio.set(r);
      },
      error: (err) => {
        this.reiniciando.set(false);
        this.confirmandoReinicio.set(false);
        this.errorReinicio.set(err?.error?.mensaje ?? 'No se pudo reiniciar el monitoreo.');
      },
    });
  }
}
