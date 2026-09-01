import { MatDatepickerIntl } from '@angular/material/datepicker';
import { MatPaginatorIntl } from '@angular/material/paginator';

export function createRussianPaginatorIntl(): MatPaginatorIntl {
  const intl = new MatPaginatorIntl();
  intl.itemsPerPageLabel = 'Записей на странице:';
  intl.nextPageLabel = 'Следующая страница';
  intl.previousPageLabel = 'Предыдущая страница';
  intl.firstPageLabel = 'Первая страница';
  intl.lastPageLabel = 'Последняя страница';
  intl.getRangeLabel = (page: number, pageSize: number, length: number): string => {
    const safeLength = Math.max(length, 0);
    if (safeLength === 0 || pageSize === 0) {
      return `0 из ${safeLength}`;
    }
    const startIndex = page * pageSize;
    const endIndex = Math.min(startIndex + pageSize, safeLength);
    return `${startIndex + 1}–${endIndex} из ${safeLength}`;
  };
  return intl;
}

export function createRussianDatepickerIntl(): MatDatepickerIntl {
  const intl = new MatDatepickerIntl();
  intl.calendarLabel = 'Календарь';
  intl.openCalendarLabel = 'Открыть календарь';
  intl.closeCalendarLabel = 'Закрыть календарь';
  intl.prevMonthLabel = 'Предыдущий месяц';
  intl.nextMonthLabel = 'Следующий месяц';
  intl.prevYearLabel = 'Предыдущий год';
  intl.nextYearLabel = 'Следующий год';
  intl.prevMultiYearLabel = 'Предыдущие 24 года';
  intl.nextMultiYearLabel = 'Следующие 24 года';
  intl.switchToMonthViewLabel = 'Выбрать дату';
  intl.switchToMultiYearViewLabel = 'Выбрать месяц и год';
  intl.startDateLabel = 'Дата начала';
  intl.endDateLabel = 'Дата окончания';
  intl.comparisonDateLabel = 'Сравниваемый диапазон';
  intl.formatYearRangeLabel = (start: string, end: string): string => `${start}–${end}`;
  return intl;
}
