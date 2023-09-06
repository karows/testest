import { Injectable } from '@angular/core';
import {DashboardStream} from "../dashboard/_components/dashboard.component";
import {TextResonse} from "../_types/text-response";
import {HttpClient} from "@angular/common/http";
import {environment} from "../../environments/environment";

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  baseUrl = environment.apiUrl;
  constructor(private httpClient: HttpClient) { }

  getDashboardStreams(visibleOnly = true) {
    return this.httpClient.get<Array<DashboardStream>>(this.baseUrl + 'account/dashboard?visibleOnly=' + visibleOnly);
  }

  updateDashboardStreamPosition(streamName: string, dashboardStreamId: number, fromPosition: number, toPosition: number) {
    return this.httpClient.post(this.baseUrl + 'account/update-dashboard-position', {streamName, dashboardStreamId, fromPosition, toPosition}, TextResonse);
  }

  updateDashboardStream(stream: DashboardStream) {
    return this.httpClient.post(this.baseUrl + 'account/update-dashboard-stream', stream, TextResonse);
  }
}
