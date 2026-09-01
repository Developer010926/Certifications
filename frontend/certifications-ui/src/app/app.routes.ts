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
    title: 'Login | Certifications',
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
        title: 'Select mode | Certifications',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/mode-selection/mode-selection.component').then(
            (module) => module.ModeSelectionComponent,
          ),
      },
      {
        path: 'me',
        title: 'My page | Certifications',
        loadComponent: () =>
          import('./features/my-page/my-page.component').then((module) => module.MyPageComponent),
      },
      {
        path: 'admin/certifications',
        title: 'Certification overview | Certifications',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/certifications/certification-overview.component').then(
            (module) => module.CertificationOverviewComponent,
          ),
      },
      {
        path: 'admin/certifications/:certificationId',
        title: 'Certification | Certifications',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/certification-form/certification-detail.component').then(
            (module) => module.CertificationDetailComponent,
          ),
      },
      {
        path: 'admin/users',
        title: 'Employees | Certifications',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/employees/employee-list.component').then(
            (module) => module.EmployeeListComponent,
          ),
      },
      {
        path: 'admin/users/new',
        title: 'Create employee | Certifications',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/employee-create/employee-create.component').then(
            (module) => module.EmployeeCreateComponent,
          ),
      },
      {
        path: 'admin/users/:employeeId/contract',
        title: 'Contract | Certifications',
        canActivate: [adminGuard],
        loadComponent: () =>
          import('./features/employee-details/contract-management.component').then(
            (module) => module.ContractManagementComponent,
          ),
      },
      {
        path: 'admin/users/:employeeId',
        title: 'Employee details | Certifications',
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
    title: 'Page not found | Certifications',
    loadComponent: () =>
      import('./features/not-found/not-found.component').then((module) => module.NotFoundComponent),
  },
];
