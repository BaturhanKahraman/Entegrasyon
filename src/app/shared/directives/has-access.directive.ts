import { Directive, Input, TemplateRef, ViewContainerRef } from '@angular/core';
import { SecurityService } from 'src/app/core/services/security.service';
import { NoPermissionComponent } from '../components/no-permission/no-permission.component';

@Directive({
  selector: '[hasAccess]'
})
export class HasAccessDirective {
  constructor(private templateRef: TemplateRef<any>,
    private viewContainer: ViewContainerRef,
    private securityService:SecurityService) { }

    @Input() set hasAccess(permission:string){
      console.log(`yetki: ${permission}`)

      if(this.securityService.checkPermission(permission)){
        console.log(`yetkisi var ${permission}`)
        this.viewContainer.createEmbeddedView(this.templateRef);
      }else{
        console.log(`yetkisi yok ${permission}`)
        this.viewContainer.clear();
        this.viewContainer.createComponent(NoPermissionComponent)
      }
    }

}
