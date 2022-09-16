import {
  AfterViewInit,
  Component,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { map, Subscription, tap } from 'rxjs';
import { ApplicationLogService } from 'src/app/core/services/application-log.service';
import { ApplicationLogDetail } from 'src/app/shared/models/application-log-detail.model';
import { LogAction, LogType } from 'src/app/shared/models/log.enum';

@Component({
  selector: 'app-application-log-list',
  templateUrl: './application-log-list.component.html',
  styleUrls: ['./application-log-list.component.css'],
})
export class ApplicationLogListComponent
  implements OnInit,  OnDestroy
{
  isLoadingResult: boolean = false;
  displayedColumns = [
    'id',
    'content',
    'userInfos',
    'createdAt',
    'ipAddress',
    'logAction',
    'logType',
  ];
  data: ApplicationLogDetail[];
  dataSource = new MatTableDataSource<ApplicationLogDetail>();
  subscription: Subscription;
  LogType = LogType;
  LogAction = LogAction;
  totalItemCount: number;
  currentPageIndex:number;
  selectedPageSize = 50;
  constructor(private logService: ApplicationLogService) {}
  ngOnInit(): void {
    this.getLogs(1, this.selectedPageSize);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
  getLogs(page = 1, itemCount = 50) {
    this.isLoadingResult = true;
    
    this.subscription = this.logService
      .getPaginatedLogs(page, itemCount)
      .pipe(
        map((x) => x.data),
        tap((x) => {
          this.currentPageIndex=page-1;
          
          this.totalItemCount = x.totalItemCount;
        }),
        map((x) => x.items)
      )
      .subscribe((x) => {
        this.data = x;
        this.dataSource.data = this.data;
        this.isLoadingResult = false;
      });
  }
  handlePage(pageEvent: PageEvent) {
    console.log(pageEvent);
    this.selectedPageSize = pageEvent.pageSize;
    this.getLogs(pageEvent.pageIndex + 1, pageEvent.pageSize);
  }
}
