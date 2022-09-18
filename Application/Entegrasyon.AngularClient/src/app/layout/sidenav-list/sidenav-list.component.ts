import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { Observable } from 'rxjs';
import { MenuService } from 'src/app/core/services/menu.service';
import { SideNavItemsModel } from './sidenav-items.model';

@Component({
  selector: 'app-sidenav-list',
  templateUrl: './sidenav-list.component.html',
  styleUrls: ['./sidenav-list.component.scss']
})
export class SidenavListComponent implements OnInit {
  upMenu$:Observable<SideNavItemsModel[]>;
  downMenu$:Observable<SideNavItemsModel[]>;
  @Output('clicked') clicked= new EventEmitter();
  constructor(private menuService:MenuService) { }

  ngOnInit(): void {
    this.upMenu$=this.menuService.getUpMenus();
    this.downMenu$ = this.menuService.getDownMenus();
  }

  closeSidenav(){
    this.clicked.emit();
  }
}
