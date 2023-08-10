import { Component, OnInit, NgModule, Inject, OnDestroy, ChangeDetectorRef } from '@angular/core';
import {
  UntypedFormBuilder,
  UntypedFormGroup,
  AbstractControl,
  Validators,
  FormsModule,
  FormGroup,
} from '@angular/forms';
import { CommonModule, KeyValue } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { ReactiveFormsModule } from '@angular/forms';
import { FormCreationService } from 'src/app/core/services/formCreation.service';
import { BehaviorSubject, Subscription, mapTo } from 'rxjs';
import { DirectivesModule } from '../../../../core/directives/directives.module';
import { CustomValidationService } from 'src/app/core/services/customValidation.service';
import { ApplicantOption, FileCategory, FileUpload, SupportStatus } from 'src/app/core/api/models';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { CoreModule } from 'src/app/core/core.module';
import { DFAApplicationMainService } from 'src/app/feature-components/dfa-application-main/dfa-application-main.service';
import { DFAApplicationMainDataService } from 'src/app/feature-components/dfa-application-main/dfa-application-main-data.service';
import { AttachmentService } from 'src/app/core/api/services';
import { DFAApplicationStartDataService } from 'src/app/feature-components/dfa-application-start/dfa-application-start-data.service';
import { MatTab } from '@angular/material/tabs';

@Component({
  selector: 'app-supporting-documents',
  templateUrl: './supporting-documents.component.html',
  styleUrls: ['./supporting-documents.component.scss']
})
export default class SupportingDocumentsComponent implements OnInit, OnDestroy {
  insuranceTemplateForm: UntypedFormGroup;
  insuranceTemplateForm$: Subscription;
  rentalAgreementForm: UntypedFormGroup;
  rentalAgreementForm$: Subscription;
  identificationForm: UntypedFormGroup;
  identificationForm$: Subscription;
  insuranceTemplateDataSource = new MatTableDataSource();
  rentalAgreementDataSource = new MatTableDataSource();
  identificationDataSource = new MatTableDataSource();
  supportingDocumentsForm: UntypedFormGroup;
  formBuilder: UntypedFormBuilder;
  supportingDocumentsForm$: Subscription;
  supportingFilesForm: UntypedFormGroup;
  supportingFilesForm$: Subscription;
  formCreationService: FormCreationService;
  showSupportingFileForm: boolean = false;
  supportingFilesDataSource = new MatTableDataSource();
  documentSummaryColumnsToDisplay = [ 'fileName', 'fileDescription', 'fileType', 'uploadedDate', 'icons']
  documentSummaryDataSource = new MatTableDataSource();
  allowedFileTypes = [
    'application/pdf',
    'image/jpg',
    'image/jpeg',
    'image/png',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    'application/vnd.ms-powerpoint',
    'application/vnd.openxmlformats-officedocument.presentationml.presentation',
    'application/vnd.ms-excel',
    'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
  ];
  FileCategories = FileCategory;
  showOtherDocuments: boolean = false;
  isResidentialTenant: boolean = false;
  AppOptions = ApplicantOption;
  vieworedit: string;

  constructor(
    @Inject('formBuilder') formBuilder: UntypedFormBuilder,
    @Inject('formCreationService') formCreationService: FormCreationService,
    public customValidator: CustomValidationService,
    private dfaApplicationMainService: DFAApplicationMainService,
    private dfaApplicationMainDataService: DFAApplicationMainDataService,
    private attachmentsService: AttachmentService
  ) {
    this.formBuilder = formBuilder;
    this.formCreationService = formCreationService;
    this.dfaApplicationMainDataService.getDfaApplicationStart().subscribe(application => {
      this.isResidentialTenant = (application.appTypeInsurance.applicantOption == Object.keys(this.AppOptions)[Object.values(this.AppOptions).indexOf(this.AppOptions.ResidentialTenant)]);
    });
  }

  ngOnInit(): void {
    this.vieworedit = this.dfaApplicationMainDataService.getViewOrEdit();
    this.supportingDocumentsForm$ = this.formCreationService
      .getSupportingDocumentsForm()
      .subscribe((supportingDocuments) => {
        this.supportingDocumentsForm = supportingDocuments;
        this.supportingDocumentsForm.get('hasCopyOfARentalAgreementOrLease').setValue(false);
      });

    this.initInsuranceTemplate();
    this.initRentalAgreement();
    this.initIdentification();
    this.initSupportingFiles();

    // subscribe to changes for document summary
    const _documentSummaryFormArray = this.formCreationService.fileUploadsForm.value.get('fileUploads');
    _documentSummaryFormArray.valueChanges
      .pipe(
        mapTo(_documentSummaryFormArray.getRawValue())
        ).subscribe(data => this.documentSummaryDataSource.data = data.filter(x => x.deleteFlag == false));
  }

  initInsuranceTemplate() {
    // set up insurance template
    this.insuranceTemplateForm$ = this.formCreationService
    .getFileUploadsForm()
    .subscribe((fileUploads) => {
      this.insuranceTemplateForm = fileUploads;
      this.insuranceTemplateForm.addValidators([this.validateFormInsuranceTemplate]);
    });

    this.insuranceTemplateForm
      .get('addNewFileUploadIndicator')
      .valueChanges.subscribe((value) => this.updateFileUploadFormOnVisibility(this.insuranceTemplateForm.get('insuranceTemplateFileUpload')));

    const _insuranceTemplateFormArray = this.formCreationService.fileUploadsForm.value.get('fileUploads');
      _insuranceTemplateFormArray.valueChanges
        .pipe(
          mapTo(_insuranceTemplateFormArray.getRawValue())
          ).subscribe(data => this.insuranceTemplateDataSource.data = data.filter(x => x.fileType == Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.Insurance)] && x.deleteFlag == false));

    // initialize insurance template
    let fileUploads = this.formCreationService.fileUploadsForm.value.get('fileUploads').value;
    if (fileUploads.filter(x => x.fileType === Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.Insurance)]).length > 0) {
      let insuranceFoundIndex = fileUploads.findIndex(x => x.fileType === Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.Insurance)]);
      this.insuranceTemplateForm.get('insuranceTemplateFileUpload').setValue(fileUploads[insuranceFoundIndex]);
    } else {
      this.insuranceTemplateForm.get('insuranceTemplateFileUpload').reset();
      this.insuranceTemplateForm.get('insuranceTemplateFileUpload.modifiedBy').setValue("Applicant");
      this.insuranceTemplateForm.get('insuranceTemplateFileUpload.fileType').setValue(Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.Insurance)]);
      this.insuranceTemplateForm.get('addNewFileUploadIndicator').setValue(true);
      this.insuranceTemplateForm.get('insuranceTemplateFileUpload.deleteFlag').setValue(false);
      this.insuranceTemplateForm.get('insuranceTemplateFileUpload.applicationId').setValue(this.dfaApplicationMainDataService.getApplicationId());
    }
  }

  initRentalAgreement() {
    // set up rental agreement
    this.rentalAgreementForm$ = this.formCreationService
    .getFileUploadsForm()
    .subscribe((fileUploads) => {
      this.rentalAgreementForm = fileUploads;
      this.rentalAgreementForm.addValidators([this.validateFormRentalAgreement]);
    });

    this.rentalAgreementForm
      .get('addNewFileUploadIndicator')
      .valueChanges.subscribe((value) => this.updateFileUploadFormOnVisibility(this.rentalAgreementForm.get('rentalAgreementFileUpload')));

    const _rentalAgreementFormArray = this.formCreationService.fileUploadsForm.value.get('fileUploads');
      _rentalAgreementFormArray.valueChanges
        .pipe(
          mapTo(_rentalAgreementFormArray.getRawValue())
          ).subscribe(data => this.rentalAgreementDataSource.data = data.filter(x => x.fileType == Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.TenancyProof)] && x.deleteFlag == false));

    // initialize file upload form
    let fileUploads = this.formCreationService.fileUploadsForm.value.get('fileUploads').value;
    if (fileUploads.filter(x => x.fileType === Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.TenancyProof)]).length > 0) {
      let rentalAgreementFoundIndex = fileUploads.findIndex(x => x.fileType === Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.TenancyProof)]);
      this.rentalAgreementForm.get('rentalAgreementFileUpload').setValue(fileUploads[rentalAgreementFoundIndex]);
      this.supportingDocumentsForm.get('hasCopyOfARentalAgreementOrLease').setValue(true);
    } else {
      this.rentalAgreementForm.get('rentalAgreementFileUpload').reset();
      this.rentalAgreementForm.get('rentalAgreementFileUpload.modifiedBy').setValue("Applicant");
      this.rentalAgreementForm.get('rentalAgreementFileUpload.fileType').setValue(Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.TenancyProof)]);
      this.rentalAgreementForm.get('addNewFileUploadIndicator').setValue(true);
      this.rentalAgreementForm.get('rentalAgreementFileUpload.deleteFlag').setValue(false);
      this.rentalAgreementForm.get('rentalAgreementFileUpload.applicationId').setValue(this.dfaApplicationMainDataService.getApplicationId());

    }
  }

  initIdentification() {
    this.identificationForm$ = this.formCreationService
    .getFileUploadsForm()
    .subscribe((fileUploads) => {
      this.identificationForm = fileUploads;
      this.identificationForm.addValidators([this.validateFormIdentification]);
    });

    this.identificationForm
      .get('addNewFileUploadIndicator')
      .valueChanges.subscribe((value) => this.updateFileUploadFormOnVisibility(this.identificationForm.get('identificationFileUpload')));

    const _identificationFormArray = this.formCreationService.fileUploadsForm.value.get('fileUploads');
      _identificationFormArray.valueChanges
        .pipe(
          mapTo(_identificationFormArray.getRawValue())
          ).subscribe(data => this.identificationDataSource.data = data.filter(x => x.fileType == Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.Identification)] && x.deleteFlag == false));

    // initialize file upload form
    let fileUploads = this.formCreationService.fileUploadsForm.value.get('fileUploads').value;
    if (fileUploads.filter(x => x.fileType === Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.Identification)]).length > 0) {
      let identificationFoundIndex = fileUploads.findIndex(x => x.fileType === Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.Identification)]);
      this.identificationForm.get('identificationFileUpload').setValue(fileUploads[identificationFoundIndex]);
    } else {
      this.identificationForm.get('identificationFileUpload').reset();
      this.identificationForm.get('identificationFileUpload.modifiedBy').setValue("Applicant");
      this.identificationForm.get('identificationFileUpload.fileType').setValue(Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.Identification)]);
      this.identificationForm.get('addNewFileUploadIndicator').setValue(true);
      this.identificationForm.get('identificationFileUpload.deleteFlag').setValue(false);
      this.identificationForm.get('identificationFileUpload.applicationId').setValue(this.dfaApplicationMainDataService.getApplicationId());
    }
  }

  // Preserve original property order
  originalOrder = (a: KeyValue<number,string>, b: KeyValue<number,string>): number => {
    return 0;
  }

  validateFormInsuranceTemplate(form: FormGroup) {
    let FileCategories = FileCategory;

    let supportingFiles = form.get('fileUploads')?.getRawValue();
    if (supportingFiles?.filter(x => x.fileType === "Insurance" && x.deleteFlag == false).length <= 0) {
      return { noInsuranceTemplate: true };
    }
    return null;
  }

  validateFormRentalAgreement(form: FormGroup) {
    let FileCategories = FileCategory;

    let supportingFiles = form.get('fileUploads')?.getRawValue();
    if (supportingFiles?.filter(x => x.fileType === "TenancyProof" && x.deleteFlag == false).length <= 0) {
      return { noRentalAgreement: true };
    }
    return null;
  }

  validateFormIdentification(form: FormGroup) {
    let FileCategories = FileCategory;

    let supportingFiles = form.get('fileUploads')?.getRawValue();
    if (supportingFiles?.filter(x => x.fileType === "Identification" && x.deleteFlag == false).length <= 0) {
      return { noIdentification: true };
    }
    return null;
  }

  initSupportingFiles(): void {
    this.supportingFilesForm$ = this.formCreationService
    .getFileUploadsForm()
    .subscribe((fileUploads) => {
      this.supportingFilesForm = fileUploads;
    });

    this.supportingFilesForm
      .get('addNewFileUploadIndicator')
      .valueChanges.subscribe((value) => this.updateFileUploadFormOnVisibility(this.supportingFilesForm.get('supportingFilesFileUpload')));

    const _supportingFilesFormArray = this.formCreationService.fileUploadsForm.value.get('fileUploads');
      _supportingFilesFormArray.valueChanges
        .pipe(
          mapTo(_supportingFilesFormArray.getRawValue())
          ).subscribe(data => this.supportingFilesDataSource.data = data.filter(x => [this.FileCategories.Financial, this.FileCategories.Identification, Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.TenancyProof)], Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.ThirdPartyConsent)]].indexOf(x.fileType) >= 0 && x.deleteFlag == false));

    this.supportingFilesForm.get('supportingFilesFileUpload').reset();
    this.supportingFilesForm.get('supportingFilesFileUpload.modifiedBy').setValue("Applicant");
    this.showSupportingFileForm = !this.showSupportingFileForm;
    this.supportingFilesForm.get('addNewFileUploadIndicator').setValue(true);
    this.supportingFilesForm.get('supportingFilesFileUpload.deleteFlag').setValue(false);
    this.supportingFilesForm.get('supportingFilesFileUpload.applicationId').setValue(this.dfaApplicationMainDataService.getApplicationId());
  }

  saveSupportingFiles(fileUpload: FileUpload): void {
    if (this.supportingFilesForm.get('supportingFilesFileUpload').status === 'VALID') {
      let fileUploads = this.formCreationService.fileUploadsForm.value.get('fileUploads').value;
      this.attachmentsService.attachmentUpsertDeleteAttachment({body: fileUpload }).subscribe({
        next: (fileUploadId) => {
          fileUpload.id = fileUploadId;
          fileUploads.push(fileUpload);
          this.formCreationService.fileUploadsForm.value.get('fileUploads').setValue(fileUploads);
          this.showSupportingFileForm = !this.showSupportingFileForm;
          if (fileUpload.fileType == Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.TenancyProof)])
            this.supportingDocumentsForm.get('hasCopyOfARentalAgreementOrLease').setValue(true);
        },
        error: (error) => {
          console.error(error);
        }
      });
    } else {
      this.supportingFilesForm.get('supportingFilesFileUpload').markAllAsTouched();
    }
  }

  saveRequiredForm(fileUpload: FileUpload): void {
    let fileUploads = this.formCreationService.fileUploadsForm.value.get('fileUploads').value;
    if (fileUploads.filter(x => x.fileType === fileUpload.fileType).length > 0) {
      this.attachmentsService.attachmentUpsertDeleteAttachment({body: fileUpload }).subscribe({
        next: (result) => {
          let typeFoundIndex = fileUploads.findIndex(x => x.fileType === fileUpload.fileType);
          fileUploads[typeFoundIndex] = fileUpload;
          this.formCreationService.fileUploadsForm.value.get('fileUploads').setValue(fileUploads);
        },
        error: (error) => {
          console.error(error);
        }
      });
    } else {
      this.attachmentsService.attachmentUpsertDeleteAttachment({body: fileUpload }).subscribe({
        next: (fileUploadId) => {
          fileUpload.id = fileUploadId;
          fileUploads.push(fileUpload);
          this.formCreationService.fileUploadsForm.value.get('fileUploads').setValue(fileUploads);
          if (fileUpload.fileType == Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.TenancyProof)])
            this.supportingDocumentsForm.get('hasCopyOfARentalAgreementOrLease').setValue(true);
        },
        error: (error) => {
          console.error(error);
        }
      });
    }
  }

  cancelSupportingFiles(): void {
    this.showSupportingFileForm = !this.showSupportingFileForm;
    this.supportingFilesForm.get('addNewFileUploadIndicator').setValue(false);
  }

  deleteDocumentSummaryRow(element): void {
    var form = (element.fileType == "Insurance" ? this.insuranceTemplateForm :
    (element.fileType == "Identification" ? this.identificationForm :
    (element.fileType == "TenancyProof" ? this.rentalAgreementForm : null)));
    if (form != null) {
      let fileUploads = this.formCreationService.fileUploadsForm.value.get('fileUploads').value;
      let foundIndex = fileUploads.findIndex(x => x.fileType === element.fileType);
      element.deleteFlag = true;
      this.attachmentsService.attachmentUpsertDeleteAttachment({body: element}).subscribe({
        next: (result) => {
          fileUploads[foundIndex] = element;
          this.formCreationService.fileUploadsForm.value.get('fileUploads').setValue(fileUploads);
        },
        error: (error) => {
          console.error(error);
        }
      });
    } else if (element.fileType === Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.DamagePhoto)]) {
      this.dfaApplicationMainService.deleteDamagePhoto.emit(element);
    } else if (element.fileType === this.FileCategories.Cleanup) {
      this.dfaApplicationMainService.deleteCleanupLog.emit(element);
    } else {
      let fileUploads = this.formCreationService.fileUploadsForm.value.get('fileUploads').value;
      let index = fileUploads.indexOf(element);
      element.deleteFlag = true;
      this.attachmentsService.attachmentUpsertDeleteAttachment({body: element}).subscribe({
        next: (result) => {
          fileUploads[index] = element;
          this.formCreationService.fileUploadsForm.value.get('fileUploads').setValue(fileUploads);
          if (fileUploads.filter(x => x.fileType == Object.keys(this.FileCategories)[Object.values(this.FileCategories).indexOf(this.FileCategories.TenancyProof)])?.length == 0)
            this.supportingDocumentsForm.get('hasCopyOfARentalAgreementOrLease').setValue(false);
          if (this.formCreationService.fileUploadsForm.value.get('fileUploads').value.length === 0) {
            this.supportingFilesForm
              .get('addNewFileUploadIndicator')
              .setValue(false);
          }
        },
        error: (error) => {
          console.error(error);
        }
      });
    }
  }

  updateFileUploadFormOnVisibility(form: AbstractControl): void {
    form.get('fileName').updateValueAndValidity();
    form.get('fileDescription').updateValueAndValidity();
    form.get('fileType').updateValueAndValidity();
    form.get('uploadedDate').updateValueAndValidity();
    form.get('modifiedBy').updateValueAndValidity();
    form.get('fileData').updateValueAndValidity();
  }

  /**
   * Returns the control of the form
   */
  get supportingFilesFormControl(): { [key: string]: AbstractControl } {
    return this.supportingFilesForm.controls;
  }
  get insuranceTemplateFormControl(): { [key: string]: AbstractControl } {
    return this.insuranceTemplateForm.controls;
  }
  get rentalAgreementFormControl(): { [key: string]: AbstractControl } {
    return this.rentalAgreementForm.controls;
  }
  get identificationFormControl(): { [key: string]: AbstractControl} {
    return this.identificationForm.controls;
  }

  ngOnDestroy(): void {
    this.supportingDocumentsForm$.unsubscribe();
    this.supportingFilesForm$.unsubscribe();
    this.insuranceTemplateForm$.unsubscribe();
  }

  public onToggleOtherDocuments() {
    this.showOtherDocuments = !this.showOtherDocuments;
  }
}

@NgModule({
  imports: [
    CommonModule,
    FormsModule,
    CoreModule,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    ReactiveFormsModule,
    DirectivesModule,
  ],
  declarations: [SupportingDocumentsComponent]
})
class SupportingDocumentsModule {}
