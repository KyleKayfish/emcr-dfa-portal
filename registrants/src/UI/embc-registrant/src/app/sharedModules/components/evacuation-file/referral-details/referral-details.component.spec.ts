import { HttpClient, HttpHandler } from '@angular/common/http';
import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ReferralDetailsComponent } from './referral-details.component';

describe('ReferralDetailsComponent', () => {
  let component: ReferralDetailsComponent;
  let fixture: ComponentFixture<ReferralDetailsComponent>;

  beforeEach(
    waitForAsync(() => {
      TestBed.configureTestingModule({
        declarations: [ReferralDetailsComponent],
        providers: [HttpClient, HttpHandler]
      }).compileComponents();
    })
  );

  beforeEach(() => {
    fixture = TestBed.createComponent(ReferralDetailsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
