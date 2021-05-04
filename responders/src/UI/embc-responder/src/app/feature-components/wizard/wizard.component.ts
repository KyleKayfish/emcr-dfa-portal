import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import {
  WizardSidenavModel
} from 'src/app/core/models/wizard-sidenav.model';
import { WizardService } from './wizard.service';

@Component({
  selector: 'app-wizard',
  templateUrl: './wizard.component.html',
  styleUrls: ['./wizard.component.scss']
})
export class WizardComponent implements OnInit {
  sideNavMenu: Array<WizardSidenavModel> = new Array<WizardSidenavModel>();

  constructor(private router: Router, private wizardService: WizardService) {
    this.sideNavMenu = this.wizardService.menuItems;
  }

  ngOnInit(): void {
    this.loadDefaultStep();
  }

  /**
   * Constructs the first step to be loaded by the wizard
   */
  loadDefaultStep(): void {
    if (this.sideNavMenu !== null && this.sideNavMenu !== undefined) {
      const firstStepUrl = this.sideNavMenu[0].route;
      const firstStepId = this.sideNavMenu[0].step;
      const firstStepTitle = this.sideNavMenu[0].title;
      this.router.navigate([firstStepUrl], {
        state: { step: firstStepId, title: firstStepTitle }
      });
    }
  }
}
