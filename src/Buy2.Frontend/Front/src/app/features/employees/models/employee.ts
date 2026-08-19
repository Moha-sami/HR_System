export interface Employee {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly joinDate: string;
  readonly jobTitle: string;
  readonly email: string;
  readonly adminAccess: 'Full' | 'Limited';
}
