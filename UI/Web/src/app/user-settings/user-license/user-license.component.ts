import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  EventEmitter,
  inject,
  Input,
  OnInit, Output
} from '@angular/core';
import {FormControl, FormGroup, Validators} from "@angular/forms";
import {AccountService} from "../../_services/account.service";
import {ScrobbleProvider, ScrobblingService} from "../../_services/scrobbling.service";
import {ToastrService} from "ngx-toastr";

@Component({
  selector: 'app-user-license',
  templateUrl: './user-license.component.html',
  styleUrls: ['./user-license.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserLicenseComponent implements OnInit {

  @Input({required: true}) hasValidLicense: boolean = false;
  @Output() validate: EventEmitter<void> = new EventEmitter<void>();

  formGroup: FormGroup = new FormGroup({});
  isViewMode: boolean = true;
  private readonly destroyRef = inject(DestroyRef);
  hasLicense: boolean = false;


  constructor(public accountService: AccountService, private scrobblingService: ScrobblingService, private toastr: ToastrService, private readonly cdRef: ChangeDetectorRef) { }

  ngOnInit(): void {
    this.formGroup.addControl('licenseKey', new FormControl('', [Validators.required]));
    this.formGroup.addControl('email', new FormControl('', [Validators.required]));
    this.accountService.currentUser$.subscribe(user => {
      if (user) {
        this.hasLicense = user.hasLicense;
        this.cdRef.markForCheck();
      }
    });
  }


  resetForm() {
    this.formGroup.get('licenseKey')?.setValue('');
    this.formGroup.get('email')?.setValue('');
    this.cdRef.markForCheck();
  }

  saveForm() {
    this.accountService.updateUserLicense(this.formGroup.get('licenseKey')!.value, this.formGroup.get('email')!.value).subscribe(isValid => {
      this.hasValidLicense = isValid;
      if (!this.hasValidLicense) {
        this.toastr.info("License Key saved, but it is not valid. Please ensure you have an active subscription");
      } else {
        this.toastr.success('KavitaPlus unlocked. Please reauthenticate to get full benefits.');
      }
      this.hasLicense = this.formGroup.get('licenseKey')!.value.length > 0;
      this.resetForm();
      this.isViewMode = true;
      this.cdRef.markForCheck();
    }, err => {

    });
  }

  toggleViewMode() {
    this.isViewMode = !this.isViewMode;
    this.resetForm();
  }

  validateLicense() {
    this.validate.emit();

  }

}
