import { createRussianDatepickerIntl, createRussianPaginatorIntl } from './russian-material-intl';

describe('Russian Angular Material labels', () => {
  it('provides Russian paginator controls and range text', () => {
    const intl = createRussianPaginatorIntl();
    expect(intl.itemsPerPageLabel).toBe('Записей на странице:');
    expect(intl.nextPageLabel).toBe('Следующая страница');
    expect(intl.getRangeLabel(1, 25, 60)).toBe('26–50 из 60');
    expect(intl.getRangeLabel(0, 25, 0)).toBe('0 из 0');
  });

  it('provides Russian datepicker accessibility labels', () => {
    const intl = createRussianDatepickerIntl();
    expect(intl.calendarLabel).toBe('Календарь');
    expect(intl.openCalendarLabel).toBe('Открыть календарь');
    expect(intl.nextMonthLabel).toBe('Следующий месяц');
  });
});
