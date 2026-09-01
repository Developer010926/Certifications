import { Routes } from '@angular/router';
import {
  activeContractGuard,
  adminGuard,
  anonymousOnlyGuard,
  authGuard,
  defaultRedirectGuard,
} from './core/guards/auth.guards';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    canActivate: [defaultRedirectGuard],
    loadComponent: () =>
      import('./features/not-found/empty-route.component').then(
        (module) => module.EmptyRouteComponent,
      ),
  },
  {
    path: 'login',
    title: 'Вход | Сертификации',
    canActivate: [anonymousOnlyGuard],
    loadComponent: () =>
      import('./features/login/login.component').then((module) => module.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard, activeContractGuard],
    loadComponent: () =>
      import('./layout/app-shell.component').then((module) => module.AppShellComponent),
    children: [
      {
        path: 'select-mode',
        title: 'Выбор режима | Сертификации',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/mode-selection/mode-selection.component').then(
            (module) => module.ModeSelectionComponent,
          ),
      },
      {
        path: 'me',
        title: 'Моя страница | Сертификации',
        loadComponent: () =>
          import('./features/my-page/my-page.component').then((module) => module.MyPageComponent),
      },
      {
        path: 'admin/certifications',
        title: 'Обзор сертификаций | Сертификации',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/certifications/certification-overview.component').then(
            (module) => module.CertificationOverviewComponent,
          ),
      },
      {
        path: 'admin/certifications/:certificationId',
        title: 'Сертификация | Сертификации',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/certification-form/certification-detail.component').then(
            (module) => module.CertificationDetailComponent,
          ),
      },
      {
        path: 'admin/users',
        title: 'Сотрудники | Сертификации',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/employees/employee-list.component').then(
            (module) => module.EmployeeListComponent,
          ),
      },
      {
        path: 'admin/users/new',
        title: 'Создание сотрудника | Сертификации',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/employee-create/employee-create.component').then(
            (module) => module.EmployeeCreateComponent,
          ),
      },
      {
        path: 'admin/users/:employeeId/contract',
        title: 'Контракт | Сертификации',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/employee-details/contract-management.component').then(
            (module) => module.ContractManagementComponent,
          ),
      },
      {
        path: 'admin/users/:employeeId',
        title: 'Сотрудник | Сертификации',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/employee-details/employee-details.component').then(
            (module) => module.EmployeeDetailsComponent,
          ),
      },
    ],
  },
  {
    path: '**',
    title: 'Страница не найдена | Сертификации',
    loadComponent: () =>
      import('./features/not-found/not-found.component').then((module) => module.NotFoundComponent),
  },
];
