import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { takeUntil, tap } from 'rxjs';
@Component({
  selector: 'app-primary-layout',
  templateUrl: './primary-layout.component.html',
  styleUrls: ['./primary-layout.component.scss']
})
export class PrimaryLayoutComponent implements OnInit {
  @ViewChild("sidenav") sideNav:ElementRef;
  ngOnInit(): void {
  }
  currentScreenSize!: string;
  isSmallScreen!:boolean;

  constructor(breakpointObserver: BreakpointObserver) {
    breakpointObserver
      .observe([Breakpoints.Small,Breakpoints.Medium,Breakpoints.XSmall])
      .subscribe((result)=>{
          this.isSmallScreen=result.matches
        }
      );
  }
}
