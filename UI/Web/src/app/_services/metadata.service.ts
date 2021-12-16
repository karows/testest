import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { of } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { ChapterMetadata } from '../_models/chapter-metadata';
import { Genre } from '../_models/genre';
import { AgeRating } from '../_models/metadata/age-rating';
import { AgeRatingDto } from '../_models/metadata/age-rating-dto';
import { Person } from '../_models/person';
import { Tag } from '../_models/tag';

@Injectable({
  providedIn: 'root'
})
export class MetadataService {

  baseUrl = environment.apiUrl;

  private ageRatingTypes: {[key: number]: string} | undefined = undefined;

  constructor(private httpClient: HttpClient) { }

  // getChapterMetadata(chapterId: number) {
  //   return this.httpClient.get<ChapterMetadata>(this.baseUrl + 'series/chapter-metadata?chapterId=' + chapterId);
  // }

  getAgeRating(ageRating: AgeRating) {
    if (this.ageRatingTypes != undefined && this.ageRatingTypes.hasOwnProperty(ageRating)) {
      return of(this.ageRatingTypes[ageRating]);
    }
    return this.httpClient.get<string>(this.baseUrl + 'series/age-rating?ageRating=' + ageRating, {responseType: 'text' as 'json'}).pipe(map(l => {
      if (this.ageRatingTypes === undefined) {
        this.ageRatingTypes = {};
      }

      this.ageRatingTypes[ageRating] = l;
      return this.ageRatingTypes[ageRating];
    }));
  }

  getAllAgeRatings(libraries?: Array<number>) {
    let method = 'metadata/age-ratings'
    if (libraries != undefined && libraries.length > 0) {
      method += '?libraryIds=' + libraries.join(',');
    }
    return this.httpClient.get<Array<AgeRatingDto>>(this.baseUrl + method);;
  }

  getAllTags(libraries?: Array<number>) {
    let method = 'metadata/tags'
    if (libraries != undefined && libraries.length > 0) {
      method += '?libraryIds=' + libraries.join(',');
    }
    return this.httpClient.get<Array<Tag>>(this.baseUrl + method);;
  }

  getAllGenres(libraries?: Array<number>) {
    let method = 'metadata/genres'
    if (libraries != undefined && libraries.length > 0) {
      method += '?libraryIds=' + libraries.join(',');
    }
    return this.httpClient.get<Genre[]>(this.baseUrl + 'metadata/genres');
  }

  getAllPeople(libraries?: Array<number>) {
    let method = 'metadata/people'
    if (libraries != undefined && libraries.length > 0) {
      method += '?libraryIds=' + libraries.join(',');
    }
    return this.httpClient.get<Person[]>(this.baseUrl + 'metadata/people');
  }
}
