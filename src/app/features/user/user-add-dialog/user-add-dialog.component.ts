import { DialogRef } from '@angular/cdk/dialog';
import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Observable, tap } from 'rxjs';
import { BranchOfficeService } from 'src/app/core/services/branch-office.service';
import { RoleService } from 'src/app/core/services/role.service';
import { UserService } from 'src/app/core/services/user.service';
import { BranchOfficeModel } from 'src/app/shared/models/branch-office.model';
import { RoleModel } from 'src/app/shared/models/role.model';
import { UserAddModel } from 'src/app/shared/models/user-add.model';

@Component({
  selector: 'app-user-add-dialog',
  templateUrl: './user-add-dialog.component.html',
  styleUrls: ['./user-add-dialog.component.css']
})
export class UserAddDialogComponent implements OnInit {
  userAddForm:FormGroup;
  isLoading:boolean = true;
  offices$:Observable<BranchOfficeModel[]>;
  roles$ : Observable<RoleModel[]>;
  constructor(private branchService:BranchOfficeService,
    private userService:UserService,
    private dialogRef:DialogRef<UserAddDialogComponent>,private snackBar:MatSnackBar,private roleService:RoleService) { }

  ngOnInit() {
    this.offices$=this.branchService.branches$;
    this.roles$ = this.roleService.roles$;
    this.initializeForm();
    this.isLoading=false;
  }

  initializeForm(){
    this.userAddForm = new FormGroup({
      userName:new FormControl(null,Validators.required),
      email:new FormControl(),
      isTwoFactorEnabled :new FormControl({disabled:true,value:false}),
      temporaryPassword:new FormControl(null,[Validators.required,Validators.maxLength(10),Validators.minLength(10)]),
      name :new FormControl(null,Validators.required),
      surname :new FormControl(null,Validators.required),
      branchOfficeId :new FormControl(null),
      roleId:new FormControl()
    });
  }
  submit(){
    if(!this.userAddForm.valid || this.isLoading)
      return;
    this.isLoading=true;
    let userModel:UserAddModel=Object.assign(this.userAddForm.value);
    this.userService.addUser(userModel)
    .pipe(tap(x=>{
      if(x.message)
        this.snackBar.open(x.message,'Tamam',{duration:5000})
    })).subscribe(x=>{
      this.userService.init();
      this.dialogRef.close();
    }).add(()=>this.isLoading=false);
  }
}
