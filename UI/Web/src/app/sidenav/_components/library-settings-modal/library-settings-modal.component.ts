import { Component, Input, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { NgbActiveModal, NgbModal } from '@ng-bootstrap/ng-bootstrap';
import { SettingsService } from 'src/app/admin/settings.service';
import { DirectoryPickerComponent, DirectoryPickerResult } from 'src/app/admin/_modals/directory-picker/directory-picker.component';
import { ConfirmService } from 'src/app/shared/confirm.service';
import { Breakpoint, UtilityService } from 'src/app/shared/_services/utility.service';
import { Library, LibraryType } from 'src/app/_models/library';
import { LibraryService } from 'src/app/_services/library.service';
import { UploadService } from 'src/app/_services/upload.service';

enum TabID {
  General = 'General',
  Folder = 'Folder',
  Cover = 'Cover',
  Advanced = 'Advanced'
}

enum StepID {
  General = 0,
  Folder = 1,
  Cover = 2,
  Advanced = 3
}

@Component({
  selector: 'app-library-settings-modal',
  templateUrl: './library-settings-modal.component.html',
  styleUrls: ['./library-settings-modal.component.scss']
})
export class LibrarySettingsModalComponent implements OnInit {

  @Input() library!: Library;

  active = TabID.General;
  imageUrls: Array<string> = [];

  libraryForm: FormGroup = new FormGroup({
    name: new FormControl<string>('', { nonNullable: true, validators: [Validators.required] }),
    type: new FormControl<LibraryType>(0, { nonNullable: true, validators: [Validators.required] })
  });

  selectedFolders: string[] = [];
  errorMessage = '';
  madeChanges = false;
  libraryTypes: string[] = []
  
  isAddLibrary = false;
  setupStep = StepID.General;

  get Breakpoint() { return Breakpoint; }
  get TabID() { return TabID; }
  get StepID() { return StepID; }

  constructor(public utilityService: UtilityService, private uploadService: UploadService, private modalService: NgbModal,
    private settingService: SettingsService, public modal: NgbActiveModal, private confirmService: ConfirmService, 
    private libraryService: LibraryService) { }

  ngOnInit(): void {

    this.settingService.getLibraryTypes().subscribe((types) => {
      this.libraryTypes = types;
    });


    if (this.library === undefined) {
      this.isAddLibrary = true;
      return;
    }

    if (this.library.coverImage != null && this.library.coverImage !== '') {
      this.imageUrls.push(this.library.coverImage);
    }

    this.setValues();
  }

  setValues() {
    if (this.library !== undefined) {
      this.libraryForm.get('name')?.setValue(this.library.name);
      this.libraryForm.get('type')?.setValue(this.library.type);
      this.selectedFolders = this.library.folders;
      this.madeChanges = false;
    }
  }

  reset() {
    this.setValues();
  }

  close(returnVal= false) {
    this.modal.close(returnVal);
  }

  async save() {
    const model = this.libraryForm.value;
    model.folders = this.selectedFolders;

    if (this.libraryForm.errors) {
      return;
    }

    if (this.library !== undefined) {
      model.id = this.library.id;
      model.folders = model.folders.map((item: string) => item.startsWith('\\') ? item.substr(1, item.length) : item);
      model.type = parseInt(model.type, 10);

      if (model.type !== this.library.type) {
        if (!await this.confirmService.confirm(`Changing library type will trigger a new scan with different parsing rules and may lead to 
        series being re-created and hence you may loose progress and bookmarks. You should backup before you do this. Are you sure you want to continue?`)) return;
      }

      this.libraryService.update(model).subscribe(() => {
        this.close(true);
      }, err => {
        this.errorMessage = err;
      });
    } else {
      model.folders = model.folders.map((item: string) => item.startsWith('\\') ? item.substr(1, item.length) : item);
      model.type = parseInt(model.type, 10);
      this.libraryService.create(model).subscribe(() => {
        this.toastr.success('Library created successfully.');
        this.toastr.info('A scan has been started.');
        this.close(true);
      }, err => {
        this.errorMessage = err;
      });
    }
  }

  nextStep() {
    this.setupStep++;
    switch(this.setupStep) {
      case StepID.Folder:
        this.active = TabID.Folder;
        break;
      case StepID.Cover:
        this.active = TabID.Cover;
        break;
      case StepID.Advanced:
        this.active = TabID.Advanced;
        break;
    }
  }

  applyCoverImage(coverUrl: string) {
    this.uploadService.updateLibraryCoverImage(this.library.id, coverUrl).subscribe(() => {});
  }

  resetCoverImage() {
    this.uploadService.updateLibraryCoverImage(this.library.id, '').subscribe(() => {});
  }

  openDirectoryPicker() {
    const modalRef = this.modalService.open(DirectoryPickerComponent, { scrollable: true, size: 'lg' });
    modalRef.closed.subscribe((closeResult: DirectoryPickerResult) => {
      if (closeResult.success) {
        if (!this.selectedFolders.includes(closeResult.folderPath)) {
          this.selectedFolders.push(closeResult.folderPath);
          this.madeChanges = true;
        }
      }
    });
  }

  removeFolder(folder: string) {
    this.selectedFolders = this.selectedFolders.filter(item => item !== folder);
    this.madeChanges = true;
  }

  isNextDisabled() {
    switch (this.setupStep) {
      case StepID.General: 
        return this.libraryForm.get('name')?.invalid || this.libraryForm.get('type')?.invalid;
      case StepID.Folder:
        return this.selectedFolders.length === 0;
      case StepID.Cover:
        return false; // Covers are optional
      case StepID.Advanced:
        return false; // Advanced are optional
    }
    return false;
  }

}
