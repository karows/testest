import { ChangeDetectionStrategy, Component, Input, OnDestroy, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { LegendPosition } from '@swimlane/ngx-charts';
import { map, Observable, shareReplay, Subject, switchMap, takeUntil, tap } from 'rxjs';
import { MangaFormatPipe } from 'src/app/pipe/manga-format.pipe';
import { Member } from 'src/app/_models/auth/member';
import { AccountService } from 'src/app/_services/account.service';
import { MemberService } from 'src/app/_services/member.service';
import { StatisticsService } from 'src/app/_services/statistics.service';

const options: Intl.DateTimeFormatOptions  = { month: "short", day: "numeric" };
const mangaFormatPipe = new MangaFormatPipe();

@Component({
  selector: 'app-read-by-day-and',
  templateUrl: './read-by-day-and.component.html',
  styleUrls: ['./read-by-day-and.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ReadByDayAndComponent implements OnInit, OnDestroy {
  /**
   * Only show for one user
   */
  @Input() userId: number = 0;

  view: [number, number] = [0, 400];
  formGroup: FormGroup = new FormGroup({
    'users': new FormControl(-1, []),
  });
  users$: Observable<Member[]>;
  data$: Observable<any>;

  private readonly onDestroy = new Subject<void>();

  constructor(private statService: StatisticsService, private memberService: MemberService, private accountService: AccountService) {
    this.users$ = this.memberService.getMembers().pipe(takeUntil(this.onDestroy), shareReplay());
    this.data$ = this.formGroup.get('users')!.valueChanges.pipe(
      switchMap(uId => this.statService.getReadCountByDay(uId)),
      map(data => {
        const gList = data.reduce((formats, entry) => {
          const formatTranslated = mangaFormatPipe.transform(entry.format);
          if (!formats[formatTranslated]) {
            formats[formatTranslated] = {
              name: formatTranslated,
              value: 0,
              series: []
            };
          }
          formats[formatTranslated].series.push({name: new Date(entry.value).toLocaleDateString("en-US", options), value: entry.count});

          return formats;
        }, {});
        return Object.keys(gList).map(format => {
          return {name: format, value: 0, series: gList[format].series}
        });
      }),
      shareReplay(),
      takeUntil(this.onDestroy),
    );
  }

  ngOnInit(): void {
    this.data$.subscribe();
    this.formGroup.get('users')?.setValue(this.userId, {emitValue: true});
    this.accountService.currentUser$.subscribe(u => {
      if (!u || !this.accountService.hasAdminRole(u)) this.formGroup.get('users')?.disable();
    });
    
  }

  ngOnDestroy(): void {
    this.onDestroy.next();
    this.onDestroy.complete();
  }

}
