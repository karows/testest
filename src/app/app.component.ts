import { Component, OnInit } from '@angular/core';
import { User } from './_models/user';
import { AccountService } from './_services/account.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

  constructor(private accountService: AccountService) {  }

  ngOnInit(): void {
    this.setCurrentUser();
  }


  setCurrentUser() {
    const userString = localStorage.getItem(this.accountService.userKey);
    if (userString !== '' || localStorage.getItem(this.accountService.userKey) !== undefined) {
      const user: User = JSON.parse(userString + '');
      this.accountService.setCurrentUser(user);
    }
  }
}
