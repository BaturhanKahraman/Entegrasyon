import { Component, NgModule } from '@angular/core';
import { UserComponent } from './user.component';
import { SharedModule } from 'src/app/shared/shared.module';
import { RouterModule, Routes } from '@angular/router';
import { UserListComponent } from './user-list/user-list.component';

const routes : Routes = [
  {path:"",component:UserComponent,children:[
    {path:"",component:UserListComponent}
  ]}
]

@NgModule({
  imports: [
    SharedModule,
    RouterModule.forChild(routes)
  ],
  declarations: [UserComponent,UserListComponent],
  exports:[RouterModule]
})
export class UserModule { }
