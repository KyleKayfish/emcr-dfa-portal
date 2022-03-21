import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SupportReferralComponent } from './support-referral.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { MatDialogModule } from '@angular/material/dialog';
import { FormBuilder } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { MaterialModule } from 'src/app/material.module';

describe('SupportReferralComponent', () => {
  let component: SupportReferralComponent;
  let fixture: ComponentFixture<SupportReferralComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        HttpClientTestingModule,
        RouterTestingModule,
        MatDialogModule,
        MaterialModule
      ],
      declarations: [SupportReferralComponent],
      providers: [FormBuilder, DatePipe]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SupportReferralComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
