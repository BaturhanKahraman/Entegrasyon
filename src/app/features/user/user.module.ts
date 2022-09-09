import { Component, NgModule } from '@angular/core';
import { UserComponent } from './user.component';
import { SharedModule } from 'src/app/shared/shared.module';
import { RouterModule, Routes } from '@angular/router';
import { UserListComponent } from './user-list/user-list.component';
import { UserAddDialogComponent } from './user-add-dialog/user-add-dialog.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

const routes : Routes = [
  {path:"",component:UserComponent,children:[
    {path:"",component:UserListComponent}
  ]}
]

@NgModule({
  imports: [
    SharedModule,
    RouterModule.forChild(routes),
    FormsModule,ReactiveFormsModule
  ],
  declarations: [UserComponent,UserListComponent,UserAddDialogComponent],
  exports:[RouterModule]
})
export class UserModule { }
