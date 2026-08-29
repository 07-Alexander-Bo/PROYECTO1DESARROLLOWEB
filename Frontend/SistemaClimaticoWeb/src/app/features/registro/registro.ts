import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth';
import { Rol } from '../../core/models';

@Component({
  selector: 'app-registro',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './registro.html',
  styleUrl: './registro.css',
})
export class Registro {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);
  private router = inject(Router);

  // Segun el CHECK constraint CK_Usuarios_Rol de tu base de datos.
  roles: Rol[] = ['Usuario', 'Operador', 'Administrador'];

  cargando = signal(false);
  error = signal<string | null>(null);
  exito = signal(false);

  form = this.fb.nonNullable.group({
    nombreCompleto: ['', [Validators.required, Validators.maxLength(150)]],
    correo: ['', [Validators.required, Validators.email, Validators.maxLength(150)]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    rol: ['Usuario' as Rol, [Validators.required]],
  });

  enviar() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.cargando.set(true);
    this.error.set(null);

    this.auth.registro(this.form.getRawValue()).subscribe({
      next: () => {
        this.cargando.set(false);
        this.exito.set(true);
        setTimeout(() => this.router.navigate(['/login']), 1200);
      },
      error: (err) => {
        this.cargando.set(false);
        this.error.set(err?.error?.mensaje ?? 'No se pudo completar el registro.');
      },
    });
  }
}
