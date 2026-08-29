import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { SensoresService } from '../../core/sensores';
import { SensorDto, TIPOS_SENSOR } from '../../core/models';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './admin.html',
  styleUrl: './admin.css',
})
export class Admin implements OnInit {
  private fb = inject(FormBuilder);
  private srv = inject(SensoresService);

  tipos = TIPOS_SENSOR;
  sensores = signal<SensorDto[]>([]);
  abierto = signal(false);
  guardando = signal(false);
  error = signal<string | null>(null);

  form = this.fb.nonNullable.group({
    nombre: ['', Validators.required],
    tipo: ['Temperatura', Validators.required],
    unidadMedida: ['', Validators.required],
    ubicacion: [''],
  });

  ngOnInit() {
    this.cargar();
  }

  cargar() {
    this.srv.listar(true).subscribe({ next: (data) => this.sensores.set(data) });
  }

  guardar() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.guardando.set(true);
    this.error.set(null);
    this.srv.crear(this.form.getRawValue() as any).subscribe({
      next: () => {
        this.guardando.set(false);
        this.abierto.set(false);
        this.form.reset({ nombre: '', tipo: 'Temperatura', unidadMedida: '', ubicacion: '' });
        this.cargar();
      },
      error: (err) => {
        this.guardando.set(false);
        this.error.set(err?.error?.mensaje ?? 'No se pudo crear el sensor.');
      },
    });
  }

  activarDesactivar(s: SensorDto) {
    const accion = s.activo ? this.srv.desactivar(s.idSensor) : this.srv.activar(s.idSensor);
    accion.subscribe({ next: () => this.cargar() });
  }
}
