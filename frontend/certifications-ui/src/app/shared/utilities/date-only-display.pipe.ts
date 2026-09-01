import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'dateOnly',
})
export class DateOnlyDisplayPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
    if (!match) {
      return '';
    }

    const year = Number(match[1]);
    const month = Number(match[2]);
    const day = Number(match[3]);
    const date = new Date(year, month - 1, day);
    if (date.getFullYear() !== year || date.getMonth() !== month - 1 || date.getDate() !== day) {
      return '';
    }

    return `${match[3]}.${match[2]}.${match[1]}`;
  }
}
