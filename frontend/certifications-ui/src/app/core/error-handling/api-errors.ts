import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl, FormGroup } from '@angular/forms';
import { UI_TEXT } from '../../shared/utilities/ui-text';

export function localizeServerValidationMessage(message: string, path = ''): string {
  if (/required/i.test(message)) {
    return UI_TEXT.required;
  }

  const normalizedPath = path.toLowerCase();
  if (normalizedPath.includes('prolongationwarningmonths')) {
    return 'Проверьте период предупреждения.';
  }
  if (normalizedPath.includes('prolongationalertmonths')) {
    return 'Проверьте критический период.';
  }
  if (normalizedPath.includes('prolongationforyears')) {
    return 'Проверьте срок продления.';
  }
  if (
    normalizedPath.includes('certificationdate') ||
    normalizedPath.includes('protocoldate') ||
    normalizedPath.includes('prolongationsend') ||
    normalizedPath.includes('prolongationreturned')
  ) {
    return 'Проверьте последовательность дат сертификации.';
  }
  return UI_TEXT.invalid;
}

export interface ApiProblem {
  readonly title?: string;
  readonly detail?: string;
  readonly code?: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

export function getApiProblem(error: unknown): ApiProblem | null {
  if (!(error instanceof HttpErrorResponse) || !error.error || typeof error.error !== 'object') {
    return null;
  }

  const body = error.error as Record<string, unknown>;
  const rawErrors = body['errors'];
  return {
    title: typeof body['title'] === 'string' ? body['title'] : undefined,
    detail: typeof body['detail'] === 'string' ? body['detail'] : undefined,
    code: typeof body['code'] === 'string' ? body['code'] : undefined,
    errors:
      rawErrors && typeof rawErrors === 'object'
        ? (rawErrors as Readonly<Record<string, readonly string[]>>)
        : undefined,
  };
}

export function applyValidationErrors(form: FormGroup, error: unknown): boolean {
  const errors = getApiProblem(error)?.errors;
  if (!errors) {
    return false;
  }

  let applied = false;
  for (const [serverPath, messages] of Object.entries(errors)) {
    const path = serverPath
      .split('.')
      .map((segment) => segment.charAt(0).toLowerCase() + segment.slice(1))
      .join('.');
    const control = form.get(path);
    if (control) {
      control.setErrors({
        ...control.errors,
        server: messages
          .map((message) => localizeServerValidationMessage(message, serverPath))
          .join(' '),
      });
      control.markAsTouched();
      applied = true;
    }
  }

  return applied;
}

export function controlError(control: AbstractControl | null, label: string): string {
  if (!control?.errors) {
    return '';
  }
  if (control.errors['server']) {
    return String(control.errors['server']);
  }
  if (control.errors['required']) {
    return `Поле «${label}» обязательно для заполнения.`;
  }
  return `Поле «${label}» содержит некорректное значение.`;
}
