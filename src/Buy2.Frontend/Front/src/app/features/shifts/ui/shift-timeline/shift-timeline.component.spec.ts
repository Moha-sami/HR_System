import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { Pipe, type PipeTransform } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import {
  ShiftTimelineComponent,
  type TimelineBlock,
} from './shift-timeline.component';

@Pipe({ name: 'translate', standalone: true })
class MockTranslatePipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

const BLOCKS: TimelineBlock[] = [
  { id: 1, start: 540, end: 840, label: 'Cashier', assigned: true },
  { id: 2, start: 600, end: 780, label: 'Manager', assigned: false },
];

/** Ticket #328: timeline canvas — lanes, actions, states. */
describe('ShiftTimelineComponent', () => {
  let fixture: ComponentFixture<ShiftTimelineComponent>;
  let component: ShiftTimelineComponent;

  async function setup(blocks: TimelineBlock[], start: number | null, end: number | null): Promise<void> {
    await TestBed.configureTestingModule({ imports: [ShiftTimelineComponent] })
      .overrideComponent(ShiftTimelineComponent, {
        remove: { imports: [TranslatePipe] },
        add: { imports: [MockTranslatePipe] },
      })
      .compileComponents();
    fixture = TestBed.createComponent(ShiftTimelineComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('blocks', blocks);
    fixture.componentRef.setInput('rangeStart', start);
    fixture.componentRef.setInput('rangeEnd', end);
    fixture.detectChanges();
  }

  function barEls(): HTMLElement[] {
    return Array.from(
      fixture.nativeElement.querySelectorAll('.timeline-bar'),
    ) as HTMLElement[];
  }

  it('should stack overlapping blocks in separate lanes', async () => {
    await setup(BLOCKS, 540, 1020);
    const tops = barEls().map((el) => el.style.top);
    expect(barEls().length).toBe(2);
    expect(new Set(tops).size).toBe(2);
  });

  it('should mark the unassigned bar gray', async () => {
    await setup(BLOCKS, 540, 1020);
    const unassigned = barEls().find((el) => el.textContent?.includes('Manager'));
    expect(unassigned?.className).toContain('bg-neutral-200');
  });

  it('should shade the uncovered gap', async () => {
    await setup(BLOCKS, 540, 1020);
    const gaps = fixture.nativeElement.querySelectorAll('.timeline-gap');
    expect(gaps.length).toBe(1);
  });

  it('should emit the block id from each bar action', async () => {
    await setup(BLOCKS, 540, 1020);
    const edited: Array<string | number> = [];
    const assigned: Array<string | number> = [];
    const deleted: Array<string | number> = [];
    component.editBlock.subscribe((id) => edited.push(id));
    component.assignBlock.subscribe((id) => assigned.push(id));
    component.deleteBlock.subscribe((id) => deleted.push(id));

    const click = (key: string): void => {
      const btn = fixture.nativeElement.querySelector(`[aria-label="${key}"]`) as HTMLElement;
      btn.click();
    };
    click('SHIFT_TEMPLATES.TIMELINE.EDIT_BLOCK');
    click('SHIFT_TEMPLATES.TIMELINE.ASSIGN_BLOCK');
    click('SHIFT_TEMPLATES.TIMELINE.DELETE_BLOCK');

    expect(edited).toEqual([1]);
    expect(assigned).toEqual([1]);
    expect(deleted).toEqual([1]);
  });

  it('should emit the dropped payload with the bar id on bar drop', async () => {
    await setup(BLOCKS, 540, 1020);
    const drops: Array<{ id: string | number; data: unknown }> = [];
    component.dropOnBlock.subscribe((d) => drops.push(d));

    component.onBarDrop({ container: { data: 2 }, item: { data: { id: 7 } } } as never);

    expect(drops).toEqual([{ id: 2, data: { id: 7 } }]);
  });

  it('should show the assignee name on the assigned bar', async () => {
    await setup(
      [{ id: 1, start: 540, end: 840, label: 'Cashier', assigned: true, assigneeName: 'Sara' }],
      540,
      1020,
    );
    expect(barEls()[0].textContent).toContain('Sara');
  });

  it('should show the no-range hint when the shift range is invalid', async () => {
    await setup(BLOCKS, null, null);
    expect(fixture.nativeElement.textContent).toContain('SHIFT_TEMPLATES.TIMELINE.NO_RANGE');
    expect(barEls().length).toBe(0);
  });

  it('should show the empty state when there are no blocks', async () => {
    await setup([], 540, 1020);
    expect(fixture.nativeElement.textContent).toContain('SHIFT_TEMPLATES.TIMELINE.EMPTY');
  });
});
