import { DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Inject, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { Observable, of, Subject, map, takeUntil, tap, zip, shareReplay, filter } from 'rxjs';
import { PageSplitOption } from 'src/app/_models/preferences/page-split-option';
import { ReaderMode } from 'src/app/_models/preferences/reader-mode';
import { ReaderService } from 'src/app/_services/reader.service';
import { LayoutMode } from '../../_models/layout-mode';
import { FITTING_OPTION, PAGING_DIRECTION } from '../../_models/reader-enums';
import { ReaderSetting } from '../../_models/reader-setting';
import { ImageRenderer } from '../../_models/renderer';
import { ManagaReaderService } from '../../_series/managa-reader.service';

/**
 * This is aimed at manga. Double page renderer but where if we have page = 10, you will see
 * page 11 page 10. 
 */
@Component({
  selector: 'app-double-reverse-renderer',
  templateUrl: './double-reverse-renderer.component.html',
  styleUrls: ['./double-reverse-renderer.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DoubleReverseRendererComponent implements OnInit, OnDestroy, ImageRenderer {


  @Input() readerSettings$!: Observable<ReaderSetting>;
  @Input() image$!: Observable<HTMLImageElement | null>;
  /**
   * The image fit class
   */
  @Input() imageFit$!: Observable<FITTING_OPTION>;  
  @Input() bookmark$!: Observable<number>;
  @Input() showClickOverlay$!: Observable<boolean>;
  @Input() pageNum$!: Observable<{pageNum: number, maxPages: number}>;

  @Input() getPage!: (pageNum: number) => HTMLImageElement;

  @Output() imageHeight: EventEmitter<number> = new EventEmitter<number>();

  imageFitClass$!: Observable<string>;
  showClickOverlayClass$!: Observable<string>;
  readerModeClass$!: Observable<string>;
  layoutClass$!: Observable<string>;
  shouldRenderSecondPage$!: Observable<boolean>;
  darkenss$: Observable<string> = of('brightness(100%)');
  layoutMode: LayoutMode = LayoutMode.Single;
  pageSplit: PageSplitOption = PageSplitOption.FitSplit;
  pageNum: number = 0;
  maxPages: number = 0;

  /**
   * Used to render a page on the canvas or in the image tag. This Image element is prefetched by the cachedImages buffer.
   * @remarks Used for rendering to screen.
   */
  leftImage = new Image();
   /**
    * Used solely for LayoutMode.Double rendering. 
    * @remarks Used for rendering to screen.
    */
  rightImage = new Image();
   /**
    * Used solely for LayoutMode.Double rendering. Will always hold the previous image to currentImage
    * @see currentImage
    */
  currentImagePrev = new Image();
   /**
    * Used solely for LayoutMode.Double rendering. Will always hold the next image to currentImage
    * @see currentImage
    */
  currentImageNext = new Image();
  /**
    * Used solely for LayoutMode.Double rendering. Will always hold the current - 2 image to currentImage
    * @see currentImage
    */
  currentImage2Behind = new Image();
  /**
   * Used solely for LayoutMode.Double rendering. Will always hold the current + 2 image to currentImage
   * @see currentImage
   */
  currentImage2Ahead = new Image();

  /**
   * Determines if we should render a double page.
   * The general gist is if we are on double layout mode, the current page (first page) is not a cover image or a wide image 
   * and the next page is not a wide image (as only non-wides should be shown next to each other).
   * @remarks This will always fail if the window's width is greater than the height
  */
  shouldRenderDouble$!: Observable<boolean>;

  pageSpreadMap: {[key: number]: 'W'|'S'} = {};

  private readonly onDestroy = new Subject<void>();

  get ReaderMode() {return ReaderMode;} 
  get FITTING_OPTION() {return FITTING_OPTION;} 
  get LayoutMode() {return LayoutMode;} 

  

  constructor(private readonly cdRef: ChangeDetectorRef, public mangaReaderService: ManagaReaderService, 
    @Inject(DOCUMENT) private document: Document, public readerService: ReaderService) { }

  ngOnInit(): void {
    this.readerModeClass$ = this.readerSettings$.pipe(
      filter(_ => this.isValid()),
      map(values => values.readerMode), 
      map(mode => mode === ReaderMode.LeftRight || mode === ReaderMode.UpDown ? '' : 'd-none'),
      takeUntil(this.onDestroy)
    );

    this.darkenss$ = this.readerSettings$.pipe(
      filter(_ => this.isValid()),
      map(values => 'brightness(' + values.darkness + '%)'), 
      takeUntil(this.onDestroy)
    );

    this.showClickOverlayClass$ = this.showClickOverlay$.pipe(
      filter(_ => this.isValid()),
      map(showOverlay => showOverlay ? 'blur' : ''), 
      takeUntil(this.onDestroy)
    );

    this.pageNum$.pipe(
      takeUntil(this.onDestroy),
      filter(_ => this.isValid()),
      tap(pageInfo => {
        this.pageNum = pageInfo.pageNum;
        this.maxPages = pageInfo.maxPages;

        this.leftImage = this.getPage(this.pageNum);
        this.rightImage = this.getPage(this.pageNum + 1);

        this.currentImageNext = this.getPage(this.pageNum + 1);
        this.currentImagePrev = this.getPage(this.pageNum - 1);

        this.currentImage2Behind = this.getPage(this.pageNum - 2);
        this.currentImage2Ahead = this.getPage(this.pageNum + 2);

        this.leftImage.addEventListener('load', () => {
          this.updatePageMap(this.leftImage)
        });
        this.rightImage.addEventListener('load', () => {
          this.updatePageMap(this.leftImage)
        });
        this.currentImageNext.addEventListener('load', () => {
          this.updatePageMap(this.leftImage)
        });
        this.currentImagePrev.addEventListener('load', () => {
          this.updatePageMap(this.leftImage)
        });
        this.currentImage2Behind.addEventListener('load', () => {
          this.updatePageMap(this.leftImage)
        });
        this.currentImage2Ahead.addEventListener('load', () => {
          this.updatePageMap(this.leftImage)
        });
      })).subscribe(() => {});

    this.shouldRenderDouble$ = this.pageNum$.pipe(
      takeUntil(this.onDestroy),
      filter(_ => this.isValid()),
      map((_) => this.shouldRenderDouble()),
      shareReplay()
    );

    this.layoutClass$ = zip(this.shouldRenderDouble$, this.imageFit$).pipe(
      takeUntil(this.onDestroy),
      filter(_ => this.isValid()),
      map((value) =>  {
        if (!value[0]) return 'd-none';
        if (value[0] && value[1] === FITTING_OPTION.WIDTH) return 'fit-to-width-double-offset';
        if (value[0] && value[1] === FITTING_OPTION.HEIGHT) return 'fit-to-height-double-offset';
        if (value[0] && value[1] === FITTING_OPTION.ORIGINAL) return 'original-double-offset';
        return '';
      })
    );

    this.shouldRenderSecondPage$ = this.pageNum$.pipe(
      takeUntil(this.onDestroy),
      filter(_ => this.isValid()),
      map(_ => {
        // This should never occur
        if (this.rightImage.src === '') {
          console.log('Not rendering second page as 2nd image is empty');
          return false;
        }
        if (this.mangaReaderService.isCoverImage(this.pageNum)) {
          console.log('Not rendering second page as on cover image');
          return false;
        }
        if (this.readerService.imageUrlToPageNum(this.rightImage.src) > this.maxPages - 1) {
          console.log('Not rendering second page as 2nd image is on last page');
          return false;
        }
        if (this.isWide(this.leftImage)) {
          console.log('Not rendering second page as right page is wide');
          return false;
        }
        if (this.isWide(this.rightImage)) {
          console.log('Not rendering second page as right page is wide');
          return false;
        }
        if (this.isWide(this.currentImageNext)) {
          console.log('Not rendering second page as next page is wide');
          return false;
        }
        if (this.isWide(this.currentImagePrev)) {
          console.log('Not rendering second page as prev page is wide');
          return false;
        }

        // // Added: Unsure
        // if (this.mangaReaderService.isWideImage(this.currentImage2Ahead)) {
        //   console.log('Not rendering second page as 2 pages ahead are wide');
        //   return false;
        // }
        // if (this.mangaReaderService.isWideImage(this.currentImage2Behind)) {
        //   console.log('Not rendering second page as 2 pages behind are wide');
        //   return false;
        // }
        return true;
      }),
      //shareReplay()
    );

    this.readerSettings$.pipe(
      takeUntil(this.onDestroy),
      tap(values => {
        this.layoutMode = values.layoutMode;
        this.pageSplit = values.pageSplit;
        this.cdRef.markForCheck();
      })
    ).subscribe(() => {});

    this.bookmark$.pipe(
      takeUntil(this.onDestroy),
      filter(_ => this.isValid()),
      tap(_ => {
        const elements = [];
        const image1 = this.document.querySelector('#image-1');
        if (image1 != null) elements.push(image1);

        const image2 = this.document.querySelector('#image-2');
          if (image2 != null) elements.push(image2);

        this.mangaReaderService.applyBookmarkEffect(elements);
      })
    ).subscribe(() => {});


    this.imageFitClass$ = this.readerSettings$.pipe(
      takeUntil(this.onDestroy),
      filter(_ => this.isValid()),
      map(values => values.fitting),
      shareReplay()
    );
  }

  ngOnDestroy(): void {
    this.onDestroy.next();
    this.onDestroy.complete();
  }

  updatePageMap(img: HTMLImageElement) {
    const page = this.readerService.imageUrlToPageNum(img.src);
    if (!this.pageSpreadMap.hasOwnProperty(page)) {
      this.pageSpreadMap[page] = this.mangaReaderService.isWideImage(img) ? 'W' : 'S';
    }
  }

  /**
   * We should Render 2 pages if:
   *   1. We are not currently the first image (cover image)
   *   2. The previous page is not a cover image
   *   3. The current page is not a wide image
   *   4. The next page is not a wide image
   */
  shouldRenderDouble() {
    if (!this.isValid()) return false;

    if (this.mangaReaderService.isCoverImage(this.pageNum)) {
      console.log('Not rendering right image as is cover image');
      return false;
    }
    if (this.mangaReaderService.isCoverImage(this.pageNum - 1)) {
      console.log('Not rendering right image as current - 1 is cover image');
      return false;
    }
    if (this.isWide(this.leftImage)) {
      console.log('Not rendering right image as left is wide');
      return false;
    }
    if (this.isWide(this.rightImage)) {
      console.log('Not rendering right image as it is wide');
      return false;
    }

    if (this.isWide(this.currentImageNext)) {
      console.log('Not rendering right image as it is wide');
      return false;
    }


    return true;


    // const result =  !(
    //   this.mangaReaderService.isCoverImage(this.pageNum) 
    //   || this.mangaReaderService.isCoverImage(this.pageNum - 1)  // This is because we use prev page and hence the cover will re-show
    //   || this.mangaReaderService.isWideImage(this.leftImage) 
    //   || this.mangaReaderService.isWideImage(this.currentImageNext)
    //   );
    
    // return result;
  }

  isWide(img: HTMLImageElement) {
    const page = this.readerService.imageUrlToPageNum(img.src);
    return this.mangaReaderService.isWideImage(img) || this.pageSpreadMap.hasOwnProperty(page) && this.pageSpreadMap[page] === 'W';
  }

  isValid() {
    return this.layoutMode === LayoutMode.DoubleReversed;
  }
  
  renderPage(img: Array<HTMLImageElement | null>): void {
    if (img === null || img.length === 0 || img[0] === null) return;
    if (!this.isValid()) return;

    console.log('[DoubleRenderer] renderPage(): ', this.pageNum);
    console.log(this.readerService.imageUrlToPageNum(this.currentImage2Behind.src), this.readerService.imageUrlToPageNum(this.currentImagePrev.src),
    '[', this.readerService.imageUrlToPageNum(this.leftImage.src), ']',
    this.readerService.imageUrlToPageNum(this.currentImageNext.src), this.readerService.imageUrlToPageNum(this.currentImage2Ahead.src))
    

    // Is this really needed since the observable will prevent showing
    // if (!this.shouldRenderDouble()) {
    //   this.imageHeight.emit(this.leftImage.height);
    //   return;
    // }
    
    this.rightImage = this.currentImagePrev;

    
    this.cdRef.markForCheck();
    this.imageHeight.emit(Math.max(this.leftImage.height, this.rightImage.height));
    this.cdRef.markForCheck();
  }

  shouldMovePrev(): boolean {
    return true;
  }
  shouldMoveNext(): boolean {
    return true;
  }
  getPageAmount(direction: PAGING_DIRECTION): number {
    if (this.layoutMode !== LayoutMode.DoubleReversed) return 0;

    // If prev page:
    switch (direction) {
      case PAGING_DIRECTION.FORWARD:
        if (this.mangaReaderService.isCoverImage(this.pageNum)) {
          console.log('Moving forward 1 page as on cover image');
          return 1;
        }
        if (this.isWide(this.leftImage)) {
          console.log('Moving forward 1 page as current page is wide');
          return 1;
        }
        if (this.isWide(this.currentImageNext)) {
          console.log('Moving forward 1 page as next page is wide');
          return 1;
        }
        if (this.mangaReaderService.isSecondLastImage(this.pageNum, this.maxPages)) {
          console.log('Moving forward 1 page as 2 pages left');
          return 1;
        }
        if (this.mangaReaderService.isLastImage(this.pageNum, this.maxPages)) {
          console.log('Moving forward 1 page as 1 page left');
          return 1;
        }
        console.log('Moving forward 2 pages');
        return 2;
      case PAGING_DIRECTION.BACKWARDS:
        // When moving back and left, prev, 2 behind are all not wide, but 3 behind are, we get invalid state
        if (this.pageSpreadMap.hasOwnProperty(this.pageNum - 3) && this.pageSpreadMap[this.pageNum - 3] === 'W') {
          console.log('Moving back 1 page as 3 pages back is spread');
          return 1;
        }

        if (this.mangaReaderService.isCoverImage(this.pageNum)) {
          console.log('Moving back 1 page as on cover image');
          return 1;
        }
        if (this.isWide(this.leftImage)) {
          console.log('Moving back 1 page as current page is wide');
          return 1;
        }
        if (this.isWide(this.rightImage)) {
          console.log('Moving back 1 page as right page is wide');
          return 1;
        }
        if (this.isWide(this.currentImagePrev)) {
          console.log('Moving back 1 page as prev page is wide');
          return 1;
        }
        // if (this.mangaReaderService.isWideImage(this.currentImage2Behind)) {
        //   console.log('Moving back 1 page as 2 pages back is wide');
        //   return 1;
        // }
        // Not sure about this condition on moving backwards
        if (this.mangaReaderService.isSecondLastImage(this.pageNum, this.maxPages)) {
          console.log('Moving back 1 page as 2 pages left');
          return 1;
        }
        console.log('Moving back 2 pages');
        return 2;
    }
  }
  reset(): void {}


}
