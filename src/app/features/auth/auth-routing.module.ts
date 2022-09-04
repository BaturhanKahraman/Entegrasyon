import { Routes, RouterModule } from '@angular/router';
import { NgModule } from '@angular/core';
import { AuthComponent } from './auth.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { SetPasswordComponent } from './set-password/set-password.component';

const routes: Routes = [
    { path: '', component: AuthComponent,children:[
        {path:'login',component:LoginComponent},
        {path:'register',component:RegisterComponent},
        {path:'set-password',component:SetPasswordComponent},
    ] },
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class AuthRoutingModule {}
