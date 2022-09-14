import { DialogRef } from '@angular/cdk/dialog';
import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import {
  concatMap,
  from,
  groupBy,
  map,
  mergeMap,
  Observable,
  of,
  tap,
  toArray,
  zip,
} from 'rxjs';
import { RoleService } from 'src/app/core/services/role.service';
import { RoleClaimModel } from 'src/app/shared/models/role-claim.model';

class GroupedClaims {
  constructor(public key: string, public models: RoleClaimModel[]) {}
}
@Component({
  selector: 'app-user-role-add-dialog',
  templateUrl: './user-role-add-dialog.component.html',
  styleUrls: ['./user-role-add-dialog.component.scss'],
})
export class UserRoleAddDialogComponent implements OnInit {
  roleClaims$: Observable<RoleClaimModel[]>;
  isEdit = false;
  isLoading = true;
  roleForm: FormGroup;
  groupedClaims:any
  constructor(
    private roleService: RoleService,
    private dialogRef: DialogRef<UserRoleAddDialogComponent>
  ) {}

  ngOnInit() {
    this.roleClaims$ = this.roleService.claims$;
    this.initiliazeForm();
    this.isLoading = false;
    //this.getGroupedClaims();
  }
//todo
  getGroupedClaims() {
    this.roleClaims$
      .pipe(
        concatMap((r) =>
          from(r).pipe(
            tap((x) => (x.name = x.name.toLowerCase())),
            groupBy((x) => x.name.split('.')[0]),
            mergeMap((group$) => zip(of(group$.key), group$.pipe(toArray()))),
            map((x) => {
              return new GroupedClaims(x[0], x[1]);
            })
          )
        )
      )
      .subscribe(x=>this.groupedClaims=x);
      
  }

  initiliazeForm() {
    this.roleForm = new FormGroup({
      name: new FormControl(null, Validators.required),
      claims: new FormControl(null),
    });
  }
  submit() {
    console.log(this.roleForm);
    
  }
}
