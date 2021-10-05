import { Injectable } from '@angular/core';
import { AbstractControl } from '@angular/forms';
import { BehaviorSubject, Observable } from 'rxjs';
import { SecurityQuestion } from 'src/app/core/api/models';
import { AddressModel } from 'src/app/core/models/address.model';
import { EvacuationFileModel } from 'src/app/core/models/evacuation-file.model';
import { RegistrantProfileModel } from 'src/app/core/models/registrant-profile.model';
import { TabStatusManager } from 'src/app/core/models/tab.model';
import { WizardSidenavModel } from 'src/app/core/models/wizard-sidenav.model';
import { WizardType } from 'src/app/core/models/wizard-type.model';
import { CacheService } from 'src/app/core/services/cache.service';
import { Community } from 'src/app/core/services/locations.service';
import { WizardDataService } from './wizard-data.service';

@Injectable({ providedIn: 'root' })
export class WizardService {
  private sideMenuItems: Array<WizardSidenavModel>;
  private originalObjectReference: RegistrantProfileModel | EvacuationFileModel;
  private editStatus: BehaviorSubject<TabStatusManager[]> = new BehaviorSubject<
    TabStatusManager[]
  >([]);
  public editStatus$: Observable<
    TabStatusManager[]
  > = this.editStatus.asObservable();

  constructor(
    private cacheService: CacheService,
    private wizardDataService: WizardDataService
  ) {}

  public get menuItems(): Array<WizardSidenavModel> {
    if (this.sideMenuItems === null || this.sideMenuItems === undefined)
      this.sideMenuItems = JSON.parse(this.cacheService.get('wizardMenu'));

    return this.sideMenuItems;
  }
  public set menuItems(menuItems: Array<WizardSidenavModel>) {
    this.sideMenuItems = menuItems;
    this.cacheService.set('wizardMenu', menuItems);
  }

  public setDefaultMenuItems(type: string): void {
    if (type === WizardType.NewRegistration) {
      this.menuItems = this.wizardDataService.createNewRegistrationMenu();
    } else if (type === WizardType.NewEssFile) {
      this.menuItems = this.wizardDataService.createNewESSFileMenu();
    } else if (type === WizardType.EditRegistration) {
      this.menuItems = this.wizardDataService.createEditProfileMenu();
    } else if (type === WizardType.ReviewFile) {
      this.menuItems = this.wizardDataService.createReviewFileMenu();
    } else if (type === WizardType.CompleteFile) {
      this.menuItems = this.wizardDataService.createCompleteFileMenu();
    } else if (type === WizardType.MemberRegistration) {
      this.menuItems = this.wizardDataService.createMembersProfileMenu();
    }
  }

  public setEditStatus(value: TabStatusManager) {
    const currentValue = this.editStatus.value;
    let updatedValue = [];
    for (const tab of currentValue) {
      if (tab.tabName === value.tabName) {
        const removeIndex = currentValue.indexOf(tab);
        currentValue.splice(removeIndex, 1);
      }
    }
    updatedValue = [...currentValue, value];
    this.editStatus.next(updatedValue);
  }

  /**
   * Return the index of current step of the wizard, based on the submitted URL
   *
   * @param url URL of current page, typically retrieved from this.router.url
   */
  public getCurrentStep(currentUrl: string): number {
    let curStep = -1;
    currentUrl = currentUrl.toLowerCase();

    this.menuItems.every((item, index) => {
      const route = item.route.toLowerCase();

      // If match is found, set value and stop loop
      if (currentUrl.startsWith(route)) {
        curStep = index;
        return false;
      }

      return true;
    });

    return curStep;
  }

  public setStepStatus(name: string, status: boolean): void {
    this.menuItems.map((menu) => {
      if (menu.route === name) {
        menu.isLocked = status;
      }
      return menu;
    });
  }

  /**
   * Map an address from the API to an address usable by the wizard form
   *
   * @param addressObject AddressModel object with nested objects set by UI code
   * @returns An Address that can be filled into the site's address forms
   */
  public setAddressObjectForForm(addressObject: AddressModel): AddressModel {
    const address: AddressModel = {
      addressLine1: addressObject.addressLine1,
      addressLine2: addressObject.addressLine2,
      country: addressObject.country,
      postalCode: addressObject.postalCode,
      stateProvince: addressObject.stateProvince,
      community:
        addressObject.city !== null
          ? addressObject.city
          : addressObject.community
    };

    return address;
  }

  createObjectReference<
    T extends RegistrantProfileModel,
    X extends EvacuationFileModel
  >(originalValue: T | X): void {
    this.originalObjectReference = Object.assign({}, originalValue);
  }

  hasChanged(form: { [key: string]: AbstractControl }, type: string): boolean {
    if (
      this.originalObjectReference !== null &&
      this.originalObjectReference !== undefined
    ) {
      if (type === 'personalDetails' || type === 'contactDetails') {
        const initialValue = (this
          .originalObjectReference as RegistrantProfileModel)[type];
        return Object.keys(initialValue).some((key) => {
          const formValue = form[key].value === '' ? null : form[key].value;
          return formValue !== initialValue[key];
        });
      } else if (type === 'restriction') {
        const initialValue = (this
          .originalObjectReference as RegistrantProfileModel).restriction;
        return initialValue !== form.restrictedAccess.value;
      } else if (type === 'primaryAddress' || type === 'mailingAddress') {
        const initialValue = (this
          .originalObjectReference as RegistrantProfileModel)[type];
        let addressFormValue = null;
        if (type === 'primaryAddress') {
          addressFormValue = form.address.value;
        } else if (type === 'mailingAddress') {
          addressFormValue = form.mailingAddress.value;
        }

        return this.compareAddress(addressFormValue, initialValue);
      } else if (type === 'securityQuestions') {
        const initialValue = (this
          .originalObjectReference as RegistrantProfileModel)[type];
        return this.compareSecurityQuestion(initialValue, form);
      }
    }
  }

  compareAddress(formAddress: AddressModel, incomingAddress: AddressModel) {
    if (
      formAddress.addressLine1 === incomingAddress.addressLine1 &&
      formAddress.addressLine2 === incomingAddress.addressLine2 &&
      ((formAddress.community as Community).code ===
        (incomingAddress.community as Community).code ||
        (formAddress.community as string) === incomingAddress.city) &&
      formAddress.stateProvince.code === incomingAddress.stateProvince.code &&
      formAddress.country.code === incomingAddress.country.code &&
      formAddress.postalCode === incomingAddress.postalCode
    ) {
      return false;
    } else {
      return true;
    }
  }

  compareSecurityQuestion(initialValue: Array<SecurityQuestion>, form) {
    const q1 = form.question1.value;
    const q2 = form.question2.value;
    const q3 = form.question3.value;
    if (
      q1 === initialValue[0].question &&
      q2 === initialValue[1].question &&
      q3 === initialValue[2].question
    ) {
      return false;
    } else {
      return true;
    }
  }
}
