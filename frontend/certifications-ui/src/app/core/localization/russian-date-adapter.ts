import { Injectable } from '@angular/core';
import { NativeDateAdapter } from '@angular/material/core';
import type { MatDateFormats } from '@angular/material/core';

export const RUSSIAN_DATE_FORMATS: MatDateFormats = {
  parse: {
    dateInput: 'DD.MM.YYYY',
  },
  display: {
    dateInput: { day: '2-digit', month: '2-digit', year: 'numeric' },
    monthYearLabel: { month: 'long', year: 'numeric' },
    dateA11yLabel: { day: 'numeric', month: 'long', year: 'numeric' },
    monthYearA11yLabel: { month: 'long', year: 'numeric' },
  },
};

@Injectable()
export class RussianDateAdapter extends NativeDateAdapter {
  override parse(value: unknown, parseFormat: unknown): Date | null {
    if (typeof value !== 'string') {
      return super.parse(value, parseFormat);
    }

    const normalized = value.trim();
    if (!normalized) {
      return null;
    }

    const russianMatch = /^(\d{1,2})\.(\d{1,2})\.(\d{4})$/.exec(normalized);
    if (russianMatch) {
      return this.createValidDate(
        Number(russianMatch[3]),
        Number(russianMatch[2]),
        Number(russianMatch[1]),
      );
    }

    const isoMatch = /^(\d{4})-(\d{2})-(\d{2})$/.exec(normalized);
    if (isoMatch) {
      return this.createValidDate(Number(isoMatch[1]), Number(isoMatch[2]), Number(isoMatch[3]));
    }

    return this.invalid();
  }

  private createValidDate(year: number, month: number, day: number): Date {
    const date = new Date(year, month - 1, day);
    return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
      ? date
      : this.invalid();
  }
}
