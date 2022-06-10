import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { DOCUMENT } from '@angular/common';
import { AfterViewInit, Component, ContentChild, EventEmitter, HostListener, Inject, Input, NgZone, OnChanges, OnDestroy, OnInit, Output, Renderer2, SimpleChanges, TemplateRef, ViewChild, ViewContainerRef } from '@angular/core';
import { filter, from, map, pairwise, Subject, throttleTime } from 'rxjs';
import { FilterSettings } from 'src/app/metadata-filter/filter-settings';
import { Breakpoint, UtilityService } from 'src/app/shared/_services/utility.service';
import { JumpKey } from 'src/app/_models/jumpbar/jump-key';
import { Library } from 'src/app/_models/library';
import { Pagination } from 'src/app/_models/pagination';
import { FilterEvent, FilterItem, SeriesFilter } from 'src/app/_models/series-filter';
import { ActionItem } from 'src/app/_services/action-factory.service';
import { ScrollService } from 'src/app/_services/scroll.service';
import { SeriesService } from 'src/app/_services/series.service';

const FILTER_PAG_REGEX = /[^0-9]/g;

@Component({
  selector: 'app-card-detail-layout',
  templateUrl: './card-detail-layout.component.html',
  styleUrls: ['./card-detail-layout.component.scss']
})
export class CardDetailLayoutComponent implements OnInit, OnDestroy, AfterViewInit, OnChanges {

  @Input() header: string = '';
  @Input() isLoading: boolean = false;
  @Input() items: any[] = [];
  @Input() pagination!: Pagination;
  
  // Filter Code
  @Input() filterOpen!: EventEmitter<boolean>;
  /**
   * Should filtering be shown on the page
   */
  @Input() filteringDisabled: boolean = false;
  /**
   * Any actions to exist on the header for the parent collection (library, collection)
   */
  @Input() actions: ActionItem<any>[] = [];
  @Input() trackByIdentity!: (index: number, item: any) => string;
  @Input() filterSettings!: FilterSettings;


  @Input() jumpBarKeys: Array<JumpKey> = []; // This is aprox 784 pixels wide

  @Output() itemClicked: EventEmitter<any> = new EventEmitter();
  @Output() pageChange: EventEmitter<Pagination> = new EventEmitter();
  @Output() applyFilter: EventEmitter<FilterEvent> = new EventEmitter();

  @ContentChild('cardItem') itemTemplate!: TemplateRef<any>;
  @ContentChild('noData') noDataTemplate!: TemplateRef<any>;
  @ViewChild('scroller') scroller!: CdkVirtualScrollViewport;

  itemSize: number = 5; // Idk what this actually does. Less results in more items rendering 

  filter!: SeriesFilter;
  libraries: Array<FilterItem<Library>> = [];

  updateApplied: number = 0;

  private onDestory: Subject<void> = new Subject();

  get Breakpoint() {
    return Breakpoint;
  }

  constructor(private seriesService: SeriesService, public utilityService: UtilityService, @Inject(DOCUMENT) private document: Document,
              private scrollService: ScrollService, private ngZone: NgZone) {
    this.filter = this.seriesService.createSeriesFilter();
  }

  @HostListener('window:resize', ['$event'])
  @HostListener('window:orientationchange', ['$event'])
  resizeJumpBar() {
    //console.log('resizing jump bar');
    //const breakpoint = this.utilityService.getActiveBreakpoint();
    // if (window.innerWidth < 784) {
    //   // We need to remove a few sections of keys 
    //   const len = this.jumpBarKeys.length;
    //   if (this.jumpBarKeys.length <= 8) return;
    //   this.jumpBarKeys = this.jumpBarKeys.filter((item, index) => {
    //     return index % 2 === 0;
    //   });
    // }
  }

  ngOnInit(): void {
    this.trackByIdentity = (index: number, item: any) => `${this.header}_${this.updateApplied}_${item?.libraryId}`; // ${this.pagination?.currentPage}_


    if (this.filterSettings === undefined) {
      this.filterSettings = new FilterSettings();
    }

    if (this.pagination === undefined) {
      this.pagination = {currentPage: 1, itemsPerPage: this.items.length, totalItems: this.items.length, totalPages: 1}
    }
  }

  ngAfterViewInit() {
    this.scroller.elementScrolled().pipe(
      map(() => this.scroller.measureScrollOffset('bottom')),
      pairwise(),
      filter(([y1, y2]) => (y2 < y1 && y2 < 140)),
      throttleTime(200)
    ).subscribe(() => {
      if (this.pagination.currentPage === this.pagination.totalPages) return;
      this.ngZone.run(() => {
        console.log('Load more pages');
        this.pagination.currentPage = this.pagination.currentPage + 1;
        this.pageChange.emit(this.pagination);
      });
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    console.log('Items: ', this.items.length);
  }

  ngOnDestroy() {
    this.onDestory.next();
    this.onDestory.complete();
  }


  onPageChange(page: number) {
    this.pageChange.emit(this.pagination);
  }

  selectPageStr(page: string) {
    this.pagination.currentPage = parseInt(page, 10) || 1;
    this.onPageChange(this.pagination.currentPage);
  }

  formatInput(input: HTMLInputElement) {
    input.value = input.value.replace(FILTER_PAG_REGEX, '');
  }

  performAction(action: ActionItem<any>) {
    if (typeof action.callback === 'function') {
      action.callback(action.action, undefined);
    }
  }

  applyMetadataFilter(event: FilterEvent) {
    this.applyFilter.emit(event);
    this.updateApplied++;
  }

  

  scrollTo(jumpKey: JumpKey) {
    // TODO: Figure out how to do this
    
    let targetIndex = 0;
    for(var i = 0; i < this.jumpBarKeys.length; i++) {
      if (this.jumpBarKeys[i].key === jumpKey.key) break;
      targetIndex += this.jumpBarKeys[i].size;
    }
    //console.log('scrolling to card that starts with ', jumpKey.key, + ' with index of ', targetIndex);

    // Basic implementation based on itemsPerPage being the same. 
    //var minIndex = this.pagination.currentPage * this.pagination.itemsPerPage;
    var targetPage = Math.max(Math.ceil(targetIndex / this.pagination.itemsPerPage), 1);
    //console.log('We are on page ', this.pagination.currentPage, ' and our target page is ', targetPage);
    if (targetPage === this.pagination.currentPage) {
      // Scroll to the element
      const elem = this.document.querySelector(`div[id="jumpbar-index--${targetIndex}"`);
      if (elem !== null) {
        elem.scrollIntoView({
          behavior: 'smooth'
        });
      }
      return;
    }

    this.selectPageStr(targetPage + '');

    // if (minIndex > targetIndex) {
    //   // We need to scroll forward (potentially to another page)
    // } else if (minIndex < targetIndex) {
    //   // We need to scroll back (potentially to another page)
    // }
  }
}
