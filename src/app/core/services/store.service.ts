import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { BranchOfficeModel } from 'src/app/shared/models/branch-office.model';
import { UserDetailModel } from 'src/app/shared/models/user-detail.model';
import { BranchOfficeService } from './branch-office.service';
import { UserService } from './user.service';

@Injectable()
export class StoreService {



  constructor(
    private userService: UserService,
    private branchService: BranchOfficeService
  ) {}
  init() {
    this.userService.init();
    this.branchService.init();
  }
}
