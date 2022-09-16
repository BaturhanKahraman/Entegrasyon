import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { RoleService } from 'src/app/core/services/role.service';
import { RoleModel } from 'src/app/shared/models/role.model';
import { UserRoleAddDialogComponent } from '../user-role-add-dialog/user-role-add-dialog.component';
import { UserRoleEditDialogComponent } from '../user-role-edit-dialog/user-role-edit-dialog.component';

@Component({
  selector: 'app-role-list',
  templateUrl: './role-list.component.html',
  styleUrls: ['./role-list.component.css']
})
export class RoleListComponent implements OnInit {
  roles$:Observable<RoleModel[]>;
  displayedColumns: string[] = ['id','name'];
  
  constructor(private roleService:RoleService,private dialog:MatDialog) { }

  ngOnInit() {
    this.roles$=this.roleService.roles$;
  }
  openAddDialog(){
    this.dialog.open(UserRoleAddDialogComponent,{width:'100%',data:{mode:'Add'}});
  }
  openEditDialog(roleModel:RoleModel){
    this.dialog.open(UserRoleEditDialogComponent,{width:'100%',data:roleModel})
  }
}
