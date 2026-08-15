export interface Employee {
  readonly id: number;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly phoneNumber: string;
  readonly jobRoleId: number;
  readonly roleId: number;
  readonly siteId: number;
  readonly createdAt: string;
}

export const MOCK_EMPLOYEES: readonly Employee[] = [
  { id: 1, firstName: 'Ahmed', lastName: 'Ali', email: 'a.ali@buy2.com', phoneNumber: '+966598432423', jobRoleId: 2, roleId: 1, siteId: 1, createdAt: '2026-01-15T08:00:00Z' },
  { id: 2, firstName: 'Nayef', lastName: 'Fahd', email: 'n.fahd@buy2.com', phoneNumber: '+966591234567', jobRoleId: 5, roleId: 2, siteId: 1, createdAt: '2026-01-20T08:00:00Z' },
  { id: 3, firstName: 'Sara', lastName: 'Khalid', email: 's.khalid@buy2.com', phoneNumber: '+966598765432', jobRoleId: 1, roleId: 3, siteId: 1, createdAt: '2026-02-01T08:00:00Z' },
  { id: 4, firstName: 'Mohammed', lastName: 'Hassan', email: 'm.hassan@buy2.com', phoneNumber: '+966592345678', jobRoleId: 3, roleId: 4, siteId: 2, createdAt: '2026-02-10T08:00:00Z' },
  { id: 5, firstName: 'Fatima', lastName: 'Omar', email: 'f.omar@buy2.com', phoneNumber: '+966593456789', jobRoleId: 4, roleId: 4, siteId: 2, createdAt: '2026-02-15T08:00:00Z' },
  { id: 6, firstName: 'Khalid', lastName: 'Ibrahim', email: 'k.ibrahim@buy2.com', phoneNumber: '+966594567890', jobRoleId: 6, roleId: 3, siteId: 3, createdAt: '2026-03-01T08:00:00Z' },
  { id: 7, firstName: 'Lina', lastName: 'Mohammed', email: 'l.mohammed@buy2.com', phoneNumber: '+966595678901', jobRoleId: 2, roleId: 4, siteId: 1, createdAt: '2026-03-10T08:00:00Z' },
  { id: 8, firstName: 'Youssef', lastName: 'Ahmad', email: 'y.ahmad@buy2.com', phoneNumber: '+966596789012', jobRoleId: 3, roleId: 4, siteId: 3, createdAt: '2026-03-20T08:00:00Z' },
] as const;
