import { DialogRef } from '@angular/cdk/dialog';
import { Component, Inject, Input, OnInit, ViewChild } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatOption } from '@angular/material/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSelect } from '@angular/material/select';
import { Observable } from 'rxjs';
import { RoleService } from 'src/app/core/services/role.service';
import { RoleClaimModel } from 'src/app/shared/models/role-claim.model';
import { RoleEditDto } from 'src/app/shared/models/role-edit.model';
import { RoleModel } from 'src/app/shared/models/role.model';

@Component({
  selector: 'app-user-role-edit-dialog',
  templateUrl: './user-role-edit-dialog.component.html',
  styleUrls: ['./user-role-edit-dialog.component.css'],
})
export class UserRoleEditDialogComponent implements OnInit {
  isLoading = false;
  roleForm: FormGroup;
  claims$: Observable<RoleClaimModel[]>;
  @ViewChild(MatSelect) claimSelectList: MatSelect;
  //TODO
  constructor(
    private roleService: RoleService,
    private dialogRef: DialogRef<UserRoleEditDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: RoleModel
  ) {}

  ngOnInit() {    
    this.initializeForm();
    this.claims$ = this.roleService.claims$;
    //this.patchForm();
  }
  initializeForm() {
    this.roleForm = new FormGroup({
      id: new FormControl(this.data.id),
      name: new FormControl(this.data.name, Validators.required),
      claims: new FormControl(
        this.data.claims.map((x) => x.id),
        Validators.required
      ),
    });
  }
  patchForm() {
    this.roleForm.patchValue({
      id: this.data.id,
      name: this.data.name
    });
  }
  submit() {
    if(this.isLoading===true || this.roleForm.invalid)
      return;
    this.isLoading=true;
    let claims =Array.from<string>(this.roleForm.value.claims);
    const dto:RoleEditDto = new RoleEditDto(this.roleForm.value.id,this.roleForm.value.name,claims.map(x=>+x))
    this.roleService.updateRole(dto).subscribe(x=>{
      this.dialogRef.close();
      this.roleService.init();
    }).add(()=>this.isLoading=false);
  }
  deleteRole(){
    if(this.isLoading===true)
      return;
    if(confirm(`${this.data.name} rolünü silmek istediğinizden emin misiniz ? ` )){
      this.isLoading=true;
      this.roleService.deleteRole(this.data.id).subscribe(()=>{
        this.dialogRef.close();
        this.isLoading=false
        this.roleService.init();})
    }
  }
  // getSelecteds(){
  //   this.claimSelectList.selected= this.data.claims.map<MatOption<number>>(x=>new MatOption())
  // }
}
