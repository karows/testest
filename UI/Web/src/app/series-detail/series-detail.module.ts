import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SeriesDetailRoutingModule } from './series-detail-routing.module';
import { NgbCollapseModule, NgbDropdownModule, NgbNavModule, NgbProgressbarModule, NgbRatingModule, NgbTooltipModule } from '@ng-bootstrap/ng-bootstrap';
import { SeriesMetadataDetailComponent } from './_components/series-metadata-detail/series-metadata-detail.component';
import { ReviewSeriesModalComponent } from '../_single-module/review-series-modal/review-series-modal.component';
import { SharedModule } from '../shared/shared.module';
import { TypeaheadModule } from '../typeahead/typeahead.module';
import { PipeModule } from '../pipe/pipe.module';
import { ReactiveFormsModule } from '@angular/forms';
import { SharedSideNavCardsModule } from '../shared-side-nav-cards/shared-side-nav-cards.module';
import { SeriesDetailComponent } from './_components/series-detail/series-detail.component';
import {ReviewCardComponent} from "../_single-module/review-card/review-card.component";
import {CarouselModule} from "../carousel/carousel.module";


@NgModule({
  declarations: [
    SeriesDetailComponent,
    SeriesMetadataDetailComponent,
  ],
    imports: [
        CommonModule,
        ReactiveFormsModule,

        NgbCollapseModule, // Series Metadata
        NgbNavModule,
        NgbRatingModule,
        NgbTooltipModule, // Series Detail, Extras Drawer
        NgbProgressbarModule,
        NgbDropdownModule,

        TypeaheadModule,
        PipeModule,
        SharedModule, // person badge, badge expander (these 2 can be their own module)
        SharedSideNavCardsModule,

        SeriesDetailRoutingModule,
        ReviewCardComponent,
        CarouselModule
    ]
})
export class SeriesDetailModule { }
