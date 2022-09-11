import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { RoleService } from 'src/app/core/services/role.service';
import { RoleModel } from 'src/app/shared/models/role.model';

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
   this.roleService.getClaimsFromToken();
  }

}
