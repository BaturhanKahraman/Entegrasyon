import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NavbarComponent } from './navbar.component';
import { NotificationsComponent } from './components/notifications/notifications.component';
import { ProfileBarComponent } from './components/profile-bar/profile-bar.component';
import { SharedModule } from 'src/app/shared/shared.module';

@NgModule({
  imports: [
    SharedModule
  ],
  declarations: [NavbarComponent,NotificationsComponent,ProfileBarComponent],
  exports:[NavbarComponent]
})
export class NavbarModule { }
