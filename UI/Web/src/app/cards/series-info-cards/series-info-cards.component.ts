import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component, DestroyRef,
  EventEmitter,
  inject,
  Input,
  OnChanges,
  OnInit,
  Output
} from '@angular/core';
import {debounceTime, filter, forkJoin, map} from 'rxjs';
import { FilterQueryParam } from 'src/app/shared/_services/filter-utilities.service';
import { UtilityService } from 'src/app/shared/_services/utility.service';
import { UserProgressUpdateEvent } from 'src/app/_models/events/user-progress-update-event';
import { HourEstimateRange } from 'src/app/_models/series-detail/hour-estimate-range';
import { MangaFormat } from 'src/app/_models/manga-format';
import { Series } from 'src/app/_models/series';
import { SeriesMetadata } from 'src/app/_models/metadata/series-metadata';
import { AccountService } from 'src/app/_services/account.service';
import { EVENTS, MessageHubService } from 'src/app/_services/message-hub.service';
import { ReaderService } from 'src/app/_services/reader.service';
import {takeUntilDestroyed} from "@angular/core/rxjs-interop";
import {ScrobblingService} from "../../_services/scrobbling.service";

@Component({
  selector: 'app-series-info-cards',
  templateUrl: './series-info-cards.component.html',
  styleUrls: ['./series-info-cards.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SeriesInfoCardsComponent implements OnInit, OnChanges {

  @Input({required: true}) series!: Series;
  @Input({required: true}) seriesMetadata!: SeriesMetadata;
  @Input() hasReadingProgress: boolean = false;
  @Input() readingTimeLeft: HourEstimateRange | undefined;
  /**
   * If this should make an API call to request readingTimeLeft
   */
  @Input() showReadingTimeLeft: boolean = true;
  @Output() goTo: EventEmitter<{queryParamName: FilterQueryParam, filter: any}> = new EventEmitter();

  readingTime: HourEstimateRange = {avgHours: 0, maxHours: 0, minHours: 0};
  isScrobbling: boolean = true;
  userHasLicense: boolean = false;
  libraryAllowsScrobbling: boolean = true;
  private readonly destroyRef = inject(DestroyRef);

  get MangaFormat() {
    return MangaFormat;
  }

  get FilterQueryParam() {
    return FilterQueryParam;
  }

  constructor(public utilityService: UtilityService, private readerService: ReaderService,
              private readonly cdRef: ChangeDetectorRef, private messageHub: MessageHubService,
              private accountService: AccountService, private scrobbleService: ScrobblingService) {
      // Listen for progress events and re-calculate getTimeLeft
      this.messageHub.messages$.pipe(filter(event => event.event === EVENTS.UserProgressUpdate),
                                    map(evt => evt.payload as UserProgressUpdateEvent),
                                    debounceTime(500),
                                    takeUntilDestroyed(this.destroyRef))
        .subscribe(updateEvent => {
          this.accountService.currentUser$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(user => {
            if (user === undefined || user.username !== updateEvent.username) return;
            if (updateEvent.seriesId !== this.series.id) return;
            this.getReadingTimeLeft();
          });
        });
  }

  ngOnInit(): void {
    if (this.series !== null) {
      this.getReadingTimeLeft();
      this.readingTime.minHours = this.series.minHoursToRead;
      this.readingTime.maxHours = this.series.maxHoursToRead;
      this.readingTime.avgHours = this.series.avgHoursToRead;
      this.scrobbleService.hasHold(this.series.id).subscribe(res => {
        this.isScrobbling = !res;
        this.cdRef.markForCheck();
      });

      forkJoin([
        this.scrobbleService.libraryAllowsScrobbling(this.series.id),
        this.accountService.hasValidLicense()
      ]).subscribe(results => {
        this.libraryAllowsScrobbling = results[0];
        this.userHasLicense = results[1];
        this.cdRef.markForCheck();
      });

      this.cdRef.markForCheck();
    }
  }

  ngOnChanges() {
    this.cdRef.markForCheck();
  }


  handleGoTo(queryParamName: FilterQueryParam, filter: any) {
    this.goTo.emit({queryParamName, filter});
  }

  private getReadingTimeLeft() {
    if (this.showReadingTimeLeft) this.readerService.getTimeLeft(this.series.id).subscribe((timeLeft) => {
      this.readingTimeLeft = timeLeft;
      this.cdRef.markForCheck();
    });
  }

  toggleScrobbling(evt: any) {
    evt.stopPropagation();
    if (this.isScrobbling) {
      this.scrobbleService.addHold(this.series.id).subscribe(() => {
        this.isScrobbling = !this.isScrobbling;
        this.cdRef.markForCheck();
      });
    } else {
      this.scrobbleService.removeHold(this.series.id).subscribe(() => {
        this.isScrobbling = !this.isScrobbling;
        this.cdRef.markForCheck();
      });
    }
  }
}
