import { Injectable } from '@angular/core';
import { BehaviorSubject, filter, map } from 'rxjs';
import { SideNavItemsModel, sidenavs } from 'src/app/layout/sidenav-list/sidenav-items.model';
import { RoleService } from './role.service';

@Injectable({
  providedIn: 'root'
})
export class MenuService {
menus =sidenavs;
private menuSubject =new BehaviorSubject<SideNavItemsModel[]>([]);
menus$ = this.menuSubject.asObservable();
constructor(private roleService:RoleService) { }

init(){
  this.prepareSideNavMenu();
}

prepareSideNavMenu(){
  const userClaims = this.roleService.getClaimsFromToken();
  let newArray =userClaims.map(x=>x.toLowerCase().split('.')[0]);
  let distincedClaims = [...new Set(newArray)];
  const filteredMenus=this.menus.filter(x=>distincedClaims.includes(x.claimName) || x.claimName==='')
  this.menuSubject.next(filteredMenus.sort(x=>x.order));
}

getDownMenus(){
  return this.menus$.pipe(map(x=>x.filter(z=>z.IsBottom===true)));
}
getUpMenus(){
  return this.menus$.pipe(map(x=>x.filter(z=>z.IsBottom===false)));
}

}



