import { Component, Input } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-generic-list-modal',
  templateUrl: './generic-list-modal.component.html',
  styleUrls: ['./generic-list-modal.component.scss']
})
export class GenericListModalComponent {
  @Input() items: Array<string> = [];
  @Input() title: string = '';

  listForm: FormGroup = new FormGroup({
    'filterQuery': new FormControl('', [])
  });

  filterList = (listItem: string) => {
    return listItem.toLowerCase().indexOf((this.listForm.value.filterQuery || '').toLowerCase()) >= 0;
  }

  constructor(private modal: NgbActiveModal) {}

  close() {
    this.modal.close();
  }
}
