import { Chapter } from './chapter';

export interface Volume {
    id: number;
    number: number;
    name: string;
    created: string;
    lastModified: string;
    pages: number;
    pagesRead: number;
    chapters?: Array<Chapter>;
}
