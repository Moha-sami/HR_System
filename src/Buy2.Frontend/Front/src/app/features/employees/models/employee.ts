export interface Employee {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly joinDate: string;
  readonly jobTitle: string;
  readonly email: string;
  readonly adminAccess: 'Full' | 'Limited';
}

export const MOCK_EMPLOYEES: readonly Employee[] = [
  { id: '0409', firstName: 'Ahmed', lastName: 'Mohamed', joinDate: '24-4-2024', jobTitle: 'UI Designer', email: 'ahmed.mohamed@mail.com', adminAccess: 'Full' },
  { id: '0410', firstName: 'Sara', lastName: 'Ali', joinDate: '18-3-2024', jobTitle: 'Front-end Developer', email: 'sara.ali@mail.com', adminAccess: 'Limited' },
  { id: '0411', firstName: 'Omar', lastName: 'Hassan', joinDate: '11-2-2024', jobTitle: 'Manager', email: 'omar.hassan@mail.com', adminAccess: 'Full' },
  { id: '0412', firstName: 'Fatima', lastName: 'Ibrahim', joinDate: '5-2-2024', jobTitle: 'Barista', email: 'fatima.ibrahim@mail.com', adminAccess: 'Limited' },
  { id: '0413', firstName: 'Khalid', lastName: 'Youssef', joinDate: '29-1-2024', jobTitle: 'Cashier', email: 'khalid.youssef@mail.com', adminAccess: 'Limited' },
  { id: '0414', firstName: 'Lina', lastName: 'Mahmoud', joinDate: '14-1-2024', jobTitle: 'Waiter', email: 'lina.mahmoud@mail.com', adminAccess: 'Full' },
  { id: '0415', firstName: 'Youssef', lastName: 'Ahmed', joinDate: '7-1-2024', jobTitle: 'UI/UX Designer', email: 'youssef.ahmed@mail.com', adminAccess: 'Limited' },
] as const;
