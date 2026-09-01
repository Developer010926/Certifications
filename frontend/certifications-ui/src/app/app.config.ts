import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners } from '@angular/core';
import { DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { apiErrorInterceptor } from './core/interceptors/api-error.interceptor';
import { credentialsInterceptor } from './core/interceptors/credentials.interceptor';
import { csrfInterceptor } from './core/interceptors/csrf.interceptor';
import { RUSSIAN_DATE_FORMATS, RussianDateAdapter } from './core/localization/russian-date-adapter';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([credentialsInterceptor, csrfInterceptor, apiErrorInterceptor]),
    ),
    { provide: LOCALE_ID, useValue: 'ru-RU' },
    { provide: DateAdapter, useClass: RussianDateAdapter },
    { provide: MAT_DATE_FORMATS, useValue: RUSSIAN_DATE_FORMATS },
    provideAnimationsAsync(),
  ],
};
