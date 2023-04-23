import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, OnInit, Output, inject } from '@angular/core';
import { FilterComparison } from '../../_models/filter-comparison';
import { FilterField } from '../../_models/filter-field';
import { FilterStatement } from '../../_models/filter-statement';
import { Subject } from 'rxjs';

@Component({
  selector: 'app-metadata-builder',
  templateUrl: './metadata-builder.component.html',
  styleUrls: ['./metadata-builder.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MetadataBuilderComponent implements OnInit {

  @Output() update: EventEmitter<FilterStatement[]> = new EventEmitter<FilterStatement[]>();

  private readonly cdRef = inject(ChangeDetectorRef);
  private onDestroy: Subject<void> = new Subject();
  
  createDefaultFilter() {
    return {
      comparison: FilterComparison.Equal,
      field: FilterField.SeriesName,
      value: ''
    };
  }

  filterStatements: Array<FilterStatement> = [this.createDefaultFilter()];

  
  
  updateFilter(index: number, filterStmt: FilterStatement) {
    console.log('Filter at ', index, 'updated: ', filterStmt);
    this.filterStatements[index].comparison = filterStmt.comparison;
    this.filterStatements[index].field = filterStmt.field;
    this.filterStatements[index].value = filterStmt.value; 
    //this.cdRef.markForCheck();
  }

  addFilter() {
    this.filterStatements.push(this.createDefaultFilter());
  }

  removeFilter(index: number) {
    this.filterStatements.splice(index, 1);
  }

  ngOnInit() {
    console.log('Preset: ', this.filterStatements);
  }

}
