import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/auth';
import { Rol } from '../../core/models';

// Pantalla para que un Administrador cree usuarios nuevos.
// Ya no es publica: vive dentro del shell y exige el adminGuard.
@Component({
  selector: 'app-registro',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './registro.html',
  styleUrl: './registro.css',
})
export class Registro {
  private fb = inject(FormBuilder);
  private auth = inject(AuthService);

  // Segun el CHECK constraint CK_Usuarios_Rol de tu base de datos.
  roles: Rol[] = ['Usuario', 'Operador', 'Administrador'];

  cargando = signal(false);
  error = signal<string | null>(null);
  exito = signal<string | null>(null);

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
    this.exito.set(null);

    const datos = this.form.getRawValue();

    this.auth.registro(datos).subscribe({
      next: () => {
        this.cargando.set(false);
        this.exito.set(`Usuario ${datos.correo} creado correctamente.`);
        this.form.reset({ nombreCompleto: '', correo: '', password: '', rol: 'Usuario' });
      },
      error: (err) => {
        this.cargando.set(false);
        this.error.set(
          err?.status === 403
            ? 'No tienes permiso para crear usuarios.'
            : (err?.error?.mensaje ?? 'No se pudo crear el usuario.'),
        );
      },
    });
  }
}
