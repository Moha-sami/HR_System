import { SHIFTS_ROUTES } from './shifts.routes';

/** Ticket #325: all four scheduling areas resolve under the shifts shell. */
describe('SHIFTS_ROUTES', () => {
  it('should expose shift-templates plus three coming-soon areas', () => {
    const paths = SHIFTS_ROUTES.map((r) => r.path);
    expect(paths).toContain('shift-templates');
    expect(paths).toContain('shift-management');
    expect(paths).toContain('employee-assignment');
    expect(paths).toContain('shift-market');
  });
});
