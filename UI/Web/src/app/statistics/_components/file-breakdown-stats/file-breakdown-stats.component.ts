import { ChangeDetectionStrategy, Component, OnInit, QueryList, ViewChildren } from '@angular/core';
import { FormControl } from '@angular/forms';
import { LegendPosition } from '@swimlane/ngx-charts';
import { Observable, Subject, BehaviorSubject, combineLatest, map, takeUntil, tap } from 'rxjs';
import { MangaFormatPipe } from 'src/app/pipe/manga-format.pipe';
import { MangaFormat } from 'src/app/_models/manga-format';
import { StatisticsService } from 'src/app/_services/statistics.service';
import { SortableHeader, SortEvent, compare } from 'src/app/_single-module/table/_directives/sortable-header.directive';
import { FileExtension, FileExtensionBreakdown } from '../../_models/file-breakdown';
import { PieDataItem } from '../../_models/pie-data-item';

export interface StackedBarChartDataItem {
  name: string,
  series: Array<PieDataItem>;
}

const mangaFormatPipe = new MangaFormatPipe();

@Component({
  selector: 'app-file-breakdown-stats',
  templateUrl: './file-breakdown-stats.component.html',
  styleUrls: ['./file-breakdown-stats.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class FileBreakdownStatsComponent implements OnInit {

  @ViewChildren(SortableHeader<PieDataItem>) headers!: QueryList<SortableHeader<PieDataItem>>;

  rawData$!: Observable<FileExtensionBreakdown>;
  files$!: Observable<Array<FileExtension>>;
  vizData$!: Observable<Array<StackedBarChartDataItem>>;
  private readonly onDestroy = new Subject<void>();
  
  currentSort = new BehaviorSubject<SortEvent<FileExtension>>({column: 'extension', direction: 'asc'});
  currentSort$: Observable<SortEvent<FileExtension>> = this.currentSort.asObservable();

  view: [number, number] = [700, 400];
  gradient: boolean = true;
  showLegend: boolean = true;
  showLabels: boolean = true;
  isDoughnut: boolean = false;
  legendPosition: LegendPosition = LegendPosition.Right;
  colorScheme = {
    domain: ['#5AA454', '#A10A28', '#C7B42C', '#AAAAAA']
  };

  formControl: FormControl = new FormControl(true, []);


  constructor(private statService: StatisticsService) {
    this.rawData$ = this.statService.getFileBreakdown().pipe(takeUntil(this.onDestroy));

    this.files$ = combineLatest([this.currentSort$, this.rawData$]).pipe(
      map(([sortConfig, data]) => {
        return {sortConfig, fileBreakdown: data.fileBreakdown};
      }),
      map(({ sortConfig, fileBreakdown}) => {
        return (sortConfig.column) ? fileBreakdown.sort((a: FileExtension, b: FileExtension) => {
          if (sortConfig.column === '') return 0;
          const res = compare(a[sortConfig.column], b[sortConfig.column]);
          return sortConfig.direction === 'asc' ? res : -res;
        }) : fileBreakdown;
      }),
      takeUntil(this.onDestroy)
    );

    this.vizData$ = this.files$.pipe(takeUntil(this.onDestroy), map(data => {
      const formats: {[key: string]: Array<PieDataItem>} = {};
      data.forEach(d => {
        let format = mangaFormatPipe.transform(d.format);
        if (!formats.hasOwnProperty(format)) formats[format] = [];
        formats[format].push({name: d.extension || 'Not Categorized', value: d.totalFiles, extra: d.totalSize})
      });

      const ret: Array<StackedBarChartDataItem> = [];
      Object.keys(formats).filter(k => formats.hasOwnProperty(k)).forEach(key => {
        ret.push({name: key, series: formats[key]});
      });
      console.log('processed data: ', ret);

      return ret;
    }));

    
  }

  ngOnInit(): void {
    this.onDestroy.next();
    this.onDestroy.complete();
  }

  ngOnDestroy(): void {
    this.onDestroy.next();
    this.onDestroy.complete();
  }

  onSort(evt: SortEvent<FileExtension>) {
    this.currentSort.next(evt);

    // Must clear out headers here
    this.headers.forEach((header) => {
      if (header.sortable !== evt.column) {
        header.direction = '';
      }
    });
  }

}
