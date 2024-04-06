import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import {UserCollection} from '../_models/collection-tag';
import { TextResonse } from '../_types/text-response';
import {MalStack} from "../_models/collection/mal-stack";

@Injectable({
  providedIn: 'root'
})
export class CollectionTagService {

  baseUrl = environment.apiUrl;

  constructor(private httpClient: HttpClient) { }

  allCollections(ownedOnly = false) {
    return this.httpClient.get<UserCollection[]>(this.baseUrl + 'collection?ownedOnly=' + ownedOnly);
  }

  allCollectionsForSeries(seriesId: number, ownedOnly = false) {
    return this.httpClient.get<UserCollection[]>(this.baseUrl + 'collection/all-series?ownedOnly=' + ownedOnly + '&seriesId=' + seriesId);
  }

  updateTag(tag: UserCollection) {
    return this.httpClient.post(this.baseUrl + 'collection/update', tag, TextResonse);
  }

  promoteMultipleCollections(tags: Array<number>, promoted: boolean) {
    return this.httpClient.post(this.baseUrl + 'collection/promote-multiple', {collectionIds: tags, promoted}, TextResonse);
  }

  updateSeriesForTag(tag: UserCollection, seriesIdsToRemove: Array<number>) {
    return this.httpClient.post(this.baseUrl + 'collection/update-series', {tag, seriesIdsToRemove}, TextResonse);
  }

  addByMultiple(tagId: number, seriesIds: Array<number>, tagTitle: string = '') {
    return this.httpClient.post(this.baseUrl + 'collection/update-for-series', {collectionTagId: tagId, collectionTagTitle: tagTitle, seriesIds}, TextResonse);
  }

  tagNameExists(name: string) {
    return this.httpClient.get<boolean>(this.baseUrl + 'collection/name-exists?name=' + name);
  }

  deleteTag(tagId: number) {
    return this.httpClient.delete<string>(this.baseUrl + 'collection?tagId=' + tagId, TextResonse);
  }

  deleteMultipleCollections(tags: Array<number>) {
    return this.httpClient.post(this.baseUrl + 'collection/delete-multiple', {collectionIds: tags}, TextResonse);
  }

  getMalStacks() {
    return this.httpClient.get<Array<MalStack>>(this.baseUrl + 'collection/mal-stacks');
  }
}
