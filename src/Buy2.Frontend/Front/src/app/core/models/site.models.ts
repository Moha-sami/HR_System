/**
 * Site API Response
 * Source: GET /api/v1/sites
 */
export interface Site {
  readonly id: number;
  readonly siteName: string;
  readonly latitude: number;
  readonly longitude: number;
  readonly macAddressWhitelistJson: string;
  readonly createdAt: string;
}
