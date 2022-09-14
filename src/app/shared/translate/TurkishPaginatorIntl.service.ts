import { Injectable } from "@angular/core";
import { MatPaginatorIntl } from "@angular/material/paginator";
import { Subject } from "rxjs";

@Injectable()
export class TurkishPaginatorIntl implements MatPaginatorIntl {
  changes = new Subject<void>();

  firstPageLabel = `İlk Sayfa`;
  itemsPerPageLabel = `Sayfa başına eleman:`;
  lastPageLabel = `Son Sayfa`;
  nextPageLabel = 'Sonraki Sayfa';
  previousPageLabel = 'Önceki Sayfa';

  getRangeLabel(page: number, pageSize: number, length: number): string {
    if (length === 0) {
      return `Sayfa 1`;
    }
    const amountPages = Math.ceil(length / pageSize);
    return `Sayfa ${page + 1}/${amountPages}`;
  }
}