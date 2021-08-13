import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { FormBuilder, FormControl, FormGroup } from '@angular/forms';
import { ImageService } from '../_services/image.service';
import { NgxFileDropEntry, FileSystemFileEntry, FileSystemDirectoryEntry } from 'ngx-file-drop';
import { KEY_CODES } from '../shared/_services/utility.service';
import { fromEvent, Subject } from 'rxjs';
import { takeWhile } from 'rxjs/operators';

export interface CoverImage {
  /**
   * Url of the image. If source is Url, then this is an external or existing cover image url. If it is File, it is the file handler of the image. 
   */
  imageUrl: string;
  source: 'File' | 'Url';
  /**
   * If the source is a File, the processedUrl will be the base64 encoding of the file for rendering to the screen without uploading it first
   */
  processedUrl?: string;
}

@Component({
  selector: 'app-cover-image-chooser',
  templateUrl: './cover-image-chooser.component.html',
  styleUrls: ['./cover-image-chooser.component.scss']
})
export class CoverImageChooserComponent implements OnInit, OnDestroy {

  @Input() imageUrls: Array<CoverImage> = [];
  @Output() imageUrlsChange: EventEmitter<Array<CoverImage>> = new EventEmitter<Array<CoverImage>>();

  /**
   * Should the control give the ability to select an image that emits the reset status for cover image
   */
  @Input() showReset: boolean = false;
  @Output() resetClicked: EventEmitter<void> = new EventEmitter<void>();

  /**
   * Emits the selected index. Used usually to check if something other than the default image was selected.
   */
  @Output() imageSelected: EventEmitter<number> = new EventEmitter<number>();
  /**
   * Emits a base64 encoded image
   */
  @Output() selectedBase64Url: EventEmitter<string> = new EventEmitter<string>();



  selectedIndex: number = 0;
  form!: FormGroup;
  files: NgxFileDropEntry[] = [];

  mode: 'file' | 'url' | 'all' = 'all';
  private readonly onDestroy = new Subject<void>();

  constructor(public imageService: ImageService, private fb: FormBuilder) { }

  ngOnInit(): void {
    this.form = this.fb.group({
      coverImageUrl: new FormControl('', [])
    });
  }

  ngOnDestroy() {
    this.onDestroy.next();
    this.onDestroy.complete();
  }

  getBase64Image(img: HTMLImageElement) {
    const canvas = document.createElement("canvas");
    canvas.width = img.width;
    canvas.height = img.height;
    const ctx = canvas.getContext("2d", {alpha: false});
    if (!ctx) {
      return '';
    }

    ctx.drawImage(img, 0, 0);
    var dataURL = canvas.toDataURL("image/png");
    return dataURL;
  }

  selectImage(index: number) {
    if (this.selectedIndex === index) { return; }
    this.selectedIndex = index;
    this.imageSelected.emit(this.selectedIndex);
    const selector = `.chooser img[src="${this.imageUrls[this.selectedIndex].imageUrl}"]`;

    
    const elem = document.querySelector(selector) || document.querySelectorAll('.chooser img.card-img-top')[this.selectedIndex];
    if (elem) {
      const imageElem = <HTMLImageElement>elem;
      if (imageElem.src.startsWith('data')) {
        this.selectedBase64Url.emit(imageElem.src);
        return;
      }
      const image = this.getBase64Image(imageElem);
      if (image != '') {
        this.selectedBase64Url.emit(image);
      }
    }
  }

  loadImage() {
    const url = this.form.get('coverImageUrl')?.value.trim();
    if (url && url != '') {
      const img = new Image();
      img.crossOrigin = 'anonymous';
      img.src = 'https://upload.wikimedia.org/wikipedia/en/0/0a/Aria_the_Scarlet_Ammo_vol01_Cover.jpg';
      img.onload = (e) => this.handleUrlImageAdd(e);
    }
  }
  


  

  public dropped(files: NgxFileDropEntry[]) {
    this.files = files;
    for (const droppedFile of files) {

      // Is it a file?
      if (droppedFile.fileEntry.isFile) {
        const fileEntry = droppedFile.fileEntry as FileSystemFileEntry;
        fileEntry.file((file: File) => {
          const reader  = new FileReader();
          reader.onload = (e) => this.handleFileImageAdd(e);
          reader.readAsDataURL(file);
        });
      } else {
        // It was a directory (empty directories are added, otherwise only files)
        const fileEntry = droppedFile.fileEntry as FileSystemDirectoryEntry;
        //console.log(droppedFile.relativePath, fileEntry);
      }
    }
  }

  handleFileImageAdd(e: any) {
    if (e.target == null) return;

    this.imageUrls.push({
      imageUrl: e.target.result,
      source: 'File'
    });
    this.imageUrlsChange.emit(this.imageUrls);
    this.selectedIndex += 1;
    this.imageSelected.emit(this.selectedIndex); // Auto select newly uploaded image
    this.selectedBase64Url.emit(e.target.result);
  }

  handleUrlImageAdd(e: any) {
    if (e.path === null || e.path.length === 0) return;

    const url = this.getBase64Image(e.path[0]);
    this.imageUrls.push({
      imageUrl: url,
      source: 'Url'
    });
    this.imageUrlsChange.emit(this.imageUrls);

    setTimeout(() => {
      // Auto select newly uploaded image and tell parent of new base64 url
      this.selectImage(this.selectedIndex + 1)
    });
  }

  public fileOver(event: any){
  }

  public fileLeave(event: any){
  }

  reset() {
    this.resetClicked.emit();
    this.selectedIndex = -1;
  }

  setupEnterHandler() {
    setTimeout(() => {
      const elem = document.querySelector('input[id="load-image"]');
      if (elem == null) return;
      fromEvent(elem, 'keydown')
        .pipe(takeWhile(() => this.mode === 'url')).subscribe((event) => {
          const evt = <KeyboardEvent>event;
          switch(evt.key) {
            case KEY_CODES.ENTER:
            {
              this.loadImage();
              break;
            }
      
            case KEY_CODES.ESC_KEY:
              this.mode = 'all';
              event.stopPropagation();
              break;
            default:
              break;
          }
        });
    });
  }

}
