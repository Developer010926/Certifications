import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { AuthService } from '../core/auth/auth.service';
import { CsrfTokenService } from '../core/auth/csrf-token.service';
import { MATERIAL_IMPORTS } from '../shared/material/material-imports';
import { UI_TEXT } from '../shared/utilities/ui-text';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ...MATERIAL_IMPORTS],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <mat-sidenav-container class="shell">
      <mat-sidenav
        [mode]="isCompact() ? 'over' : 'side'"
        [opened]="!isCompact() || mobileNavigationOpen()"
        (closedStart)="mobileNavigationOpen.set(false)"
      >
        <div class="brand">{{ appTitle }}</div>
        <mat-nav-list aria-label="Primary navigation">
          @if (administrationMode()) {
            <a
              mat-list-item
              routerLink="/admin/certifications"
              routerLinkActive="active-link"
              (click)="closeMobileNav()"
              >Certifications</a
            >
            <a
              mat-list-item
              routerLink="/admin/users"
              routerLinkActive="active-link"
              (click)="closeMobileNav()"
              >Employees</a
            >
          } @else {
            <a
              mat-list-item
              routerLink="/me"
              routerLinkActive="active-link"
              (click)="closeMobileNav()"
              >My page</a
            >
          }
        </mat-nav-list>
      </mat-sidenav>

      <mat-sidenav-content>
        <mat-toolbar>
          @if (isCompact()) {
            <button
              mat-button
              type="button"
              (click)="mobileNavigationOpen.set(true)"
              aria-label="Open navigation"
            >
              Menu
            </button>
          }
          <span class="toolbar-title">{{ appTitle }}</span>
          <span class="toolbar-spacer"></span>
          @if (auth.currentUser(); as user) {
            <span class="current-user">{{ user.displayName }}</span>
            @if (user.isAdmin) {
              <a mat-button routerLink="/select-mode">Switch mode</a>
            }
            <button mat-button type="button" [disabled]="loggingOut()" (click)="logout()">
              Logout
            </button>
          }
        </mat-toolbar>
        <main class="page-container" tabindex="-1"><router-outlet /></main>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: `
    .shell {
      min-height: 100dvh;
      background: var(--mat-sys-surface-container-lowest);
    }
    mat-sidenav {
      width: 15rem;
      border-radius: 0;
      border-inline-end: 1px solid var(--mat-sys-outline-variant);
    }
    .brand {
      padding: 1.35rem 1rem 1rem;
      font-size: 1.25rem;
      font-weight: 750;
      color: var(--mat-sys-primary);
    }
    .active-link {
      background: var(--mat-sys-secondary-container);
      color: var(--mat-sys-on-secondary-container);
    }
    mat-toolbar {
      position: sticky;
      top: 0;
      z-index: 10;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
    }
    .toolbar-title {
      font-weight: 700;
    }
    .toolbar-spacer {
      flex: 1;
    }
    .current-user {
      margin-inline: 1rem;
      font-size: 0.9rem;
    }
    .page-container {
      width: min(100% - 2rem, 94rem);
      margin-inline: auto;
      padding-block: 1.5rem 3rem;
      outline: none;
    }
    @media (max-width: 700px) {
      .current-user {
        display: none;
      }
      .page-container {
        width: min(100% - 1rem, 94rem);
        padding-top: 1rem;
      }
    }
  `,
})
export class AppShellComponent {
  readonly auth = inject(AuthService);
  private readonly csrf = inject(CsrfTokenService);
  private readonly router = inject(Router);
  private readonly breakpoints = inject(BreakpointObserver);
  readonly appTitle = UI_TEXT.appTitle;
  readonly mobileNavigationOpen = signal(false);
  readonly loggingOut = signal(false);
  readonly isCompact = toSignal(
    this.breakpoints.observe('(max-width: 800px)').pipe(map((result) => result.matches)),
    { initialValue: false },
  );
  private readonly currentUrl = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => event.urlAfterRedirects),
    ),
    { initialValue: this.router.url },
  );
  readonly administrationMode = computed(() => this.currentUrl().startsWith('/admin/'));

  closeMobileNav(): void {
    if (this.isCompact()) this.mobileNavigationOpen.set(false);
  }

  logout(): void {
    this.loggingOut.set(true);
    const finish = () => {
      this.csrf.clear();
      this.loggingOut.set(false);
      void this.router.navigate(['/login']);
    };
    this.auth.logout().subscribe({ next: finish, error: finish });
  }
}
