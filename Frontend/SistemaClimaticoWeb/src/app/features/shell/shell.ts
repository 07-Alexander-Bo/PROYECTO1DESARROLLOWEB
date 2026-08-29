import { Component, inject, signal, OnDestroy, PLATFORM_ID } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { AuthService } from '../../core/auth';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell implements OnDestroy {
  auth = inject(AuthService);
  private router = inject(Router);
  private platformId = inject(PLATFORM_ID);

  now = signal(new Date());
  // El reloj solo corre en el navegador: en el servidor no tiene sentido
  // y dejaria un setInterval sin limpiar en cada render SSR.
  private timer = isPlatformBrowser(this.platformId)
    ? setInterval(() => this.now.set(new Date()), 1000)
    : undefined;

  salir() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  ngOnDestroy() {
    if (this.timer) clearInterval(this.timer);
  }
}
