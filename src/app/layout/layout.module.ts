import { NgModule } from "@angular/core";
import { SharedModule } from "../shared/shared.module";
import { NavbarComponent } from "./navbar/navbar.component";
import { SidenavListComponent } from "./sidenav-list/sidenav-list.component";
import { AuthLayoutComponent } from './auth-layout/auth-layout.component';
import { PrimaryLayoutComponent } from './primary-layout/primary-layout.component';
import { RouterModule } from "@angular/router";
import { ProfileBarComponent } from "./navbar/components/profile-bar/profile-bar.component";
import { NotificationsComponent } from './navbar/components/notifications/notifications.component';
import { NavbarModule } from "./navbar/navbar.module";

@NgModule({
  declarations: [
    SidenavListComponent,
    AuthLayoutComponent,
    PrimaryLayoutComponent
    ],
  imports: [
    SharedModule,RouterModule,NavbarModule
  ],
  exports:[]
})
export class LayoutModule { }
