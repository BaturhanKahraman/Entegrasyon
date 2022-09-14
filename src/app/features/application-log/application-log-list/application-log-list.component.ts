import {
  AfterViewInit,
  Component,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import {
  MatPaginator,
  MatPaginatorSelectConfig,
} from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { map, Observable, Subject, Subscription, tap } from 'rxjs';
import { ApplicationLogService } from 'src/app/core/services/application-log.service';
import { ApplicationLogDetail } from 'src/app/shared/models/application-log-detail.model';
import { LogAction, LogType } from 'src/app/shared/models/log.enum';

@Component({
  selector: 'app-application-log-list',
  templateUrl: './application-log-list.component.html',
  styleUrls: ['./application-log-list.component.css'],
})
export class ApplicationLogListComponent
  implements  OnInit,AfterViewInit, OnDestroy
{
  @ViewChild(MatPaginator) paginator: MatPaginator;
  pageCount: number;
  totalItemCount: number;
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
  selectedPageSize = 50;
  constructor(private logService: ApplicationLogService) {}
  ngOnInit(): void {
    this.getLogs(1,this.selectedPageSize);
  }

  ngAfterViewInit(): void {
    //this.dataSource.paginator = this.paginator;
    this.paginator.page.subscribe(x=>{
      this.getLogs(x.pageIndex+1,x.pageSize);
    })
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
  getLogs(page = 1, itemCount = 50) {
    this.isLoadingResult=true;
    this.subscription = this.logService
      .getPaginatedLogs(page, itemCount)
      .pipe(
        map((x) => x.data),
        tap((x) => {
          this.pageCount = x.totalPageCount;
          this.totalItemCount = x.totalItemCount;
        }),
        map((x) => x.items)
      )
      .subscribe((x) => {
        this.data = x;
        this.dataSource.data = this.data;
        this.isLoadingResult=false;
      });
  }

}
