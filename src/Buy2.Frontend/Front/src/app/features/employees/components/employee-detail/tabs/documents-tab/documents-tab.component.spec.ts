import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { DocumentsTabComponent } from './documents-tab.component';
import { TranslatePipe } from '@ngx-translate/core';

describe('DocumentsTabComponent', () => {
  let component: DocumentsTabComponent;
  let fixture: ComponentFixture<DocumentsTabComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DocumentsTabComponent],
    })
      .overrideComponent(DocumentsTabComponent, {
        remove: { imports: [TranslatePipe] },
      })
      .compileComponents();

    fixture = TestBed.createComponent(DocumentsTabComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have comingSoon flag set to true', () => {
    expect(component.comingSoon).toBe(true);
  });
});
