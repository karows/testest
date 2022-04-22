import { Series } from "../series";

export interface RelatedSeries {
    sourceSeriesId: number;
    sequels: Array<Series>;
    prequels: Array<Series>;
    spinOffs: Array<Series>;
    adaptations: Array<Series>;
    sideStories: Array<Series>;
    characters: Array<Series>;
    contains: Array<Series>;
    others: Array<Series>;
}