import { NgModule } from '@angular/core';
import { AuthComponent } from './auth.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { AuthRoutingModule } from './auth-routing.module';
import { SharedModule } from 'src/app/shared/shared.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SetPasswordComponent } from './set-password/set-password.component';



@NgModule({
  declarations: [
    AuthComponent,
    LoginComponent,
    RegisterComponent,
    SetPasswordComponent
  ],
  imports: [
    AuthRoutingModule,SharedModule,FormsModule,ReactiveFormsModule
  ]
})
export class AuthModule { }
