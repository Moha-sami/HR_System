/**
 * Role API Response
 * Source: GET /api/v1/roles
 */
export interface Role {
  readonly id: number;
  readonly name: string;
  readonly permissionsJson: string;
  readonly createdAt: string;
}
