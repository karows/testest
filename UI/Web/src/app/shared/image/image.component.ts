import { Component, ElementRef, Input, OnChanges, OnInit, Renderer2, SimpleChanges, ViewChild } from '@angular/core';
import { ImageService } from 'src/app/_services/image.service';

/**
 * This is used for images with placeholder fallback.
 */
@Component({
  selector: 'app-image',
  templateUrl: './image.component.html',
  styleUrls: ['./image.component.scss']
})
export class ImageComponent implements OnChanges {

  /**
   * Source url to load image
   */
  @Input() imageUrl!: string;
  /**
   * Width of the image. If not defined, will not be applied
   */
  @Input() width: string = '';
  /**
   * Height of the image. If not defined, will not be applied
   */
  @Input() height: string = '';

  @ViewChild('img', {static: true}) imgElem!: ElementRef<HTMLImageElement>;

  constructor(public imageService: ImageService, private renderer: Renderer2) { }

  ngOnChanges(changes: SimpleChanges): void {
    if (this.width != '') {
      this.renderer.setStyle(this.imgElem.nativeElement, 'width', this.width);
    }

    if (this.height != '') {
      this.renderer.setStyle(this.imgElem.nativeElement, 'height', this.height);
    }
    
  }

}
