import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { UserAddDialogComponent } from './user-add-dialog/user-add-dialog.component';

@Component({
  selector: 'app-user',
  templateUrl: './user.component.html',
  styleUrls: ['./user.component.scss']
})
export class UserComponent {

  constructor(private dialog:MatDialog) { }

  openAddDialog(){
    this.dialog.open(UserAddDialogComponent,{width:'100%'});
  }
}
