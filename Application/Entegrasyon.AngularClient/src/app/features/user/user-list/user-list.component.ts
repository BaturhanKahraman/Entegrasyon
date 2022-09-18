import { AfterViewInit, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { filter, Observable } from 'rxjs';
import { StoreService } from 'src/app/core/services/store.service';
import { UserService } from 'src/app/core/services/user.service';
import { UserDetailModel } from 'src/app/shared/models/user-detail.model';
import { UserAddDialogComponent } from '../user-add-dialog/user-add-dialog.component';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss']
})
export class UserListComponent implements OnInit,AfterViewInit {
  displayedColumns: string[] = ['id','name','surname','defaultOfficeName','userName','createdAt'];
  userDetails$ : Observable<UserDetailModel[]>;
  totalUserCount$:Observable<number>;

  constructor(private userService:UserService,private dialog:MatDialog) {}

  ngOnInit() {
    this.userDetails$ = this.userService.usersDetails$
    this.totalUserCount$ = this.userService.userCount$;
  }
  ngAfterViewInit() {
  }

  applyFilter(event: Event) {
    const filterValue = (event.target as HTMLInputElement).value.toLowerCase();
  }

  openAddDialog(){
    this.dialog.open(UserAddDialogComponent,{width:'100%'});
  }
}
