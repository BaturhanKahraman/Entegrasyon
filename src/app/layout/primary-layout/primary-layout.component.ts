import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { takeUntil, tap } from 'rxjs';
import { StoreService } from 'src/app/core/services/store.service';
@Component({
  selector: 'app-primary-layout',
  templateUrl: './primary-layout.component.html',
  styleUrls: ['./primary-layout.component.scss']
})
export class PrimaryLayoutComponent implements OnInit {
  @ViewChild("sidenav") sideNav:ElementRef;
  ngOnInit(): void {
    this.store.init();
  }
  currentScreenSize!: string;
  isSmallScreen!:boolean;

  constructor(breakpointObserver: BreakpointObserver,private store:StoreService) {
    breakpointObserver
      .observe([Breakpoints.Small,Breakpoints.Medium,Breakpoints.XSmall])
      .subscribe((result)=>{
          this.isSmallScreen=result.matches
        }
      );
  }
}
