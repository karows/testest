import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { SearchResult } from '../_models/search-result';
import { AccountService } from '../_services/account.service';
import { LibraryService } from '../_services/library.service';
import { NavService } from '../_services/nav.service';

@Component({
  selector: 'app-nav-header',
  templateUrl: './nav-header.component.html',
  styleUrls: ['./nav-header.component.scss']
})
export class NavHeaderComponent implements OnInit {

  @ViewChild('search') searchViewRef!: any;

  isLoading = false;
  debounceTime = 300;
  imageStyles = {width: '24px', 'margin-top': '5px'};
  searchResults: SearchResult[] = [];
  constructor(public accountService: AccountService, private router: Router, public navService: NavService, private libraryService: LibraryService) { }

  ngOnInit(): void {
  }

  logout() {
    this.accountService.logout();
    this.router.navigateByUrl('/home');
  }

  moveFocus() {
    document.getElementById('content')?.focus();
  }

  onChangeSearch(val: string) {
      this.isLoading = true;
      this.libraryService.search(val).subscribe(results => {
        this.searchResults = results;
        this.isLoading = false;
      }, err => {
        this.searchResults = [];
      });
  }

  clickSearchResult(item: SearchResult) {
    const libraryId = item.libraryId;
    const seriesId = item.seriesId;
    this.searchViewRef.clear();
    this.searchResults = [];
    this.router.navigate(['library', libraryId, 'series', seriesId]);
  }
}
