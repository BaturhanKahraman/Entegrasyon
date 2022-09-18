import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { BranchOfficeModel } from 'src/app/shared/models/branch-office.model';
import { UserDetailModel } from 'src/app/shared/models/user-detail.model';
import { BranchOfficeService } from './branch-office.service';
import { MenuService } from './menu.service';
import { RoleService } from './role.service';
import { SecurityService } from './security.service';
import { UserService } from './user.service';

@Injectable()
export class StoreService {



  constructor(
    private userService: UserService,
    private branchService: BranchOfficeService,
    private roleService:RoleService,
    private menuService:MenuService,
    private securityService:SecurityService
  ) {}
  init() {
    this.userService.init();
    this.branchService.init();
    this.roleService.init();
    this.securityService.init();
    this.menuService.init();
  }
}
