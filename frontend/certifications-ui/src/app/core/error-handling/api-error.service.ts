import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UI_TEXT } from '../../shared/utilities/ui-text';
import { getApiProblem } from './api-errors';

const API_PROBLEM_MESSAGES: Readonly<Record<string, string>> = {
  'auth.invalid_credentials': UI_TEXT.invalidCredentials,
  'auth.active_contract_required': 'Для доступа к системе необходим активный контракт.',
  'employee.personal_id_conflict': 'Этот табельный номер уже используется.',
  'password.not_provisioned': 'Пароль сотрудника ещё не создан.',
  'contract.active_already_exists': 'У сотрудника уже есть активный контракт.',
  'contract.concurrency_conflict':
    'Контракт был изменён другим пользователем. Обновите данные и повторите попытку.',
  'certification.in_progress_exists': 'Сначала завершите текущую сертификацию.',
  'certification.already_completed': 'Завершённую сертификацию нельзя изменить.',
  'certification.stage_missing': 'Не заполнен предыдущий этап сертификации.',
  'certification.date_order_invalid': 'Даты сертификации должны соответствовать порядку этапов.',
};

@Injectable({ providedIn: 'root' })
export class ApiErrorService {
  private readonly snackBar = inject(MatSnackBar);

  message(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return UI_TEXT.genericError;
    }
    if (error.status === 0) {
      return UI_TEXT.networkError;
    }
    const problem = getApiProblem(error);
    if (problem?.code && API_PROBLEM_MESSAGES[problem.code]) {
      return API_PROBLEM_MESSAGES[problem.code];
    }
    if (error.status === 401) {
      return 'Необходимо войти в систему.';
    }
    if (error.status === 403) {
      return UI_TEXT.forbidden;
    }
    if (error.status === 404) {
      return 'Запрошенные данные не найдены.';
    }
    if (error.status === 409) {
      return UI_TEXT.conflict;
    }
    if (error.status === 400) {
      return 'Проверьте введённые данные.';
    }
    return UI_TEXT.genericError;
  }

  notify(error: unknown): void {
    this.snackBar.open(this.message(error), 'Закрыть', { duration: 6000 });
  }
}
