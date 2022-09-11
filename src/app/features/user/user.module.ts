import { Component, NgModule } from '@angular/core';
import { UserComponent } from './user.component';
import { SharedModule } from 'src/app/shared/shared.module';
import { RouterModule, Routes } from '@angular/router';
import { UserListComponent } from './user-list/user-list.component';
import { UserAddDialogComponent } from './user-add-dialog/user-add-dialog.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { UserRoleComponent } from './user-role/user-role.component';
import { RoleListComponent } from './user-role/role-list/role-list.component';
import { RoleAddEditComponent } from './user-role/role-add-edit/role-add-edit.component';

const routes: Routes = [{ path: '', component: UserComponent }];

@NgModule({
  imports: [
    SharedModule,
    RouterModule.forChild(routes),
    FormsModule,
    ReactiveFormsModule,
  ],
  declarations: [
    UserComponent,
    UserListComponent,
    UserAddDialogComponent,
    UserRoleComponent,
    RoleListComponent,
    RoleAddEditComponent
  ],
  exports: [RouterModule],
})
export class UserModule {}
