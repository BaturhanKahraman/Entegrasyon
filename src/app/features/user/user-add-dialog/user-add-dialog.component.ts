import { DialogRef } from '@angular/cdk/dialog';
import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { BranchOfficeService } from 'src/app/core/services/branch-office.service';
import { UserService } from 'src/app/core/services/user.service';
import { BranchOfficeModel } from 'src/app/shared/models/branch-office.model';

@Component({
  selector: 'app-user-add-dialog',
  templateUrl: './user-add-dialog.component.html',
  styleUrls: ['./user-add-dialog.component.css']
})
export class UserAddDialogComponent implements OnInit {
  userAddForm:FormGroup;
  offices$:Observable<BranchOfficeModel[]>;
  constructor(private branchService:BranchOfficeService,
    private userService:UserService,
    private dialogRef:DialogRef<UserAddDialogComponent>) { }

  ngOnInit() {
    this.offices$=this.branchService.branches$;
    this.initializeForm();
  }

  initializeForm(){
    this.userAddForm = new FormGroup({
      userName:new FormControl(null,Validators.required),
      email:new FormControl(),
      isTwoFactorEnabled :new FormControl({disabled:true,value:false}),
      temporaryPassword:new FormControl(null,[Validators.required,Validators.maxLength(10),Validators.minLength(10)]),
      name :new FormControl(null,Validators.required),
      surname :new FormControl(null,Validators.required),
      branchOfficeId :new FormControl(),
    });
  }
  submit(){

  }
}
