import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { RUSSIAN_DATE_FORMATS, RussianDateAdapter } from './russian-date-adapter';

describe('RussianDateAdapter', () => {
  let adapter: RussianDateAdapter;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [RussianDateAdapter, { provide: LOCALE_ID, useValue: 'ru-RU' }],
    });
    adapter = TestBed.inject(RussianDateAdapter);
  });

  it('parses a Russian calendar date without swapping the month and day', () => {
    const date = adapter.parse('01.09.2026', 'DD.MM.YYYY');
    expect(date?.getFullYear()).toBe(2026);
    expect(date?.getMonth()).toBe(8);
    expect(date?.getDate()).toBe(1);
  });

  it('accepts an ISO date as local calendar input', () => {
    const date = adapter.parse('2026-09-01', 'DD.MM.YYYY');
    expect(date?.getFullYear()).toBe(2026);
    expect(date?.getMonth()).toBe(8);
    expect(date?.getDate()).toBe(1);
  });

  it('formats a selected date for Russian input fields', () => {
    expect(adapter.format(new Date(2026, 8, 1), RUSSIAN_DATE_FORMATS.display.dateInput)).toBe(
      '01.09.2026',
    );
  });

  it('rejects an impossible calendar date', () => {
    const date = adapter.parse('31.02.2026', 'DD.MM.YYYY');
    expect(date && adapter.isValid(date)).toBe(false);
  });
});
