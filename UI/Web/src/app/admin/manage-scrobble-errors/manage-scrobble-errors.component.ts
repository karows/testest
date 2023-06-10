import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  DestroyRef,
  EventEmitter,
  inject,
  OnInit,
  Output,
  QueryList,
  ViewChildren
} from '@angular/core';
import {CommonModule} from '@angular/common';
import {PipeModule} from "../../pipe/pipe.module";
import {FormControl, FormGroup, ReactiveFormsModule} from "@angular/forms";
import {SharedModule} from "../../shared/shared.module";
import {compare, SortableHeader, SortEvent} from "../../_single-module/table/_directives/sortable-header.directive";
import {KavitaMediaError} from "../_models/media-error";
import {EVENTS, MessageHubService} from "../../_services/message-hub.service";
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {BehaviorSubject, filter, Observable, shareReplay} from "rxjs";
import {ScrobblingService} from "../../_services/scrobbling.service";
import {ScrobbleError} from "../../_models/scrobble-error";
import {TableModule} from "../../_single-module/table/table.module";

@Component({
  selector: 'app-manage-scrobble-errors',
  standalone: true,
  imports: [CommonModule, PipeModule, ReactiveFormsModule, SharedModule, TableModule],
  templateUrl: './manage-scrobble-errors.component.html',
  styleUrls: ['./manage-scrobble-errors.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ManageScrobbleErrorsComponent implements OnInit {
  @Output() scrobbleCount = new EventEmitter<number>();
  @ViewChildren(SortableHeader<KavitaMediaError>) headers!: QueryList<SortableHeader<KavitaMediaError>>;
  private readonly scrobbleService = inject(ScrobblingService);
  private readonly messageHub = inject(MessageHubService);
  private readonly cdRef = inject(ChangeDetectorRef);
  private readonly destroyRef = inject(DestroyRef);

  messageHubUpdate$ = this.messageHub.messages$.pipe(takeUntilDestroyed(this.destroyRef), filter(m => m.event === EVENTS.ScanSeries), shareReplay());
  currentSort = new BehaviorSubject<SortEvent<ScrobbleError>>({column: 'created', direction: 'asc'});
  currentSort$: Observable<SortEvent<ScrobbleError>> = this.currentSort.asObservable();

  data: Array<ScrobbleError> = [];
  isLoading = true;
  formGroup = new FormGroup({
    filter: new FormControl('', [])
  });


  constructor() {}

  ngOnInit() {

    this.loadData();

    this.messageHubUpdate$.subscribe(_ => this.loadData());

    this.currentSort$.subscribe(sortConfig => {
      this.data = (sortConfig.column) ? this.data.sort((a: ScrobbleError, b: ScrobbleError) => {
        if (sortConfig.column === '') return 0;
        const res = compare(a[sortConfig.column], b[sortConfig.column]);
        return sortConfig.direction === 'asc' ? res : -res;
      }) : this.data;
      this.cdRef.markForCheck();
    });
  }

  onSort(evt: any) {
    //SortEvent<KavitaMediaError>
    this.currentSort.next(evt);

    // Must clear out headers here
    this.headers.forEach((header) => {
      if (header.sortable !== evt.column) {
        header.direction = '';
      }
    });
  }

  loadData() {
    this.isLoading = true;
    this.cdRef.markForCheck();
    this.scrobbleService.getScrobbleErrors().subscribe(d => {
      this.data = d;
      this.isLoading = false;
      this.scrobbleCount.emit(d.length);
      this.cdRef.detectChanges();
    });
  }

  clear() {
    this.scrobbleService.clearScrobbleErrors().subscribe(_ => this.loadData());
  }

  filterList = (listItem: ScrobbleError) => {
    const query = (this.formGroup.get('filter')?.value || '').toLowerCase();
    return listItem.comment.toLowerCase().indexOf(query) >= 0 || listItem.details.toLowerCase().indexOf(query) >= 0;
  }
}