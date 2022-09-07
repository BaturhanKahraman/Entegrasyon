import { NgModule } from '@angular/core';
import { PreloadAllModules, PreloadingStrategy, RouterModule, Routes } from '@angular/router';
import { AuthLayoutComponent } from './layout/auth-layout/auth-layout.component';
import { PrimaryLayoutComponent } from './layout/primary-layout/primary-layout.component';
import { AuthGuard } from './shared/guards/auth.guard';

const routes: Routes = [
  {path:'',component:PrimaryLayoutComponent,canActivate:[AuthGuard],children:[
    {path:'',loadChildren:()=>import('./features/home/home.module').then(x=>x.HomeModule)},
    {path:'user',loadChildren:()=>import('./features/user/user.module').then(x=>x.UserModule)},
    {path:'support',loadChildren:()=>import('./features/support-developer/support-developer.module').then(x=>x.SupportDeveloperModule)},
    {path:'offices',loadChildren:()=>import('./features/branch-office/branch-office.module').then(x=>x.BranchOfficeModule)},
  ]},
  {path:'auth',component:AuthLayoutComponent,children:[
    {path:'',loadChildren:()=>import('./features/auth/auth.module').then(x=>x.AuthModule)}
  ]},{path:"**",redirectTo:''}
];
@NgModule({
  imports: [RouterModule.forRoot(routes,{preloadingStrategy:PreloadAllModules,anchorScrolling:"enabled",onSameUrlNavigation:"ignore"})],
  exports: [RouterModule],
})
export class AppRoutingModule {}
