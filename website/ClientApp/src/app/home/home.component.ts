import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})

export class HomeComponent {
  public shazamResult: ShazamResult = new ShazamResult();
  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    http.get<ShazamResult>(baseUrl + 'api/nowplaying').subscribe(result => {
      this.shazamResult = result;
    }, error => console.error(error));
  }
}

class ShazamResult {
  title: string = '';
  artist: string = '';
  success: boolean = false;
  url: string = '';
  imageUrl: string = '';
  retryMs: number = 0;
  detectedTime: string = '';
}
