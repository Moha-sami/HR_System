import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { environment } from '../../../../environments/environment';
import { RewardService } from './reward.service';

const REWARDS_URL = `${environment.baseUrl}/rewards`;

describe('RewardService', () => {
  let service: RewardService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), RewardService],
    });
    service = TestBed.inject(RewardService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should GET paginated rewards with filter query params', () => {
    service
      .getRewards({
        page: 2,
        pageSize: 10,
        search: 'voucher',
        status: 'Active',
        fromDate: '2026-09-01T00:00:00.000Z',
        toDate: '2026-09-01T23:59:59.999Z',
        sortBy: 'points',
        sortDescending: true,
      })
      .subscribe((response) => {
        expect(response.totalCount).toBe(1);
        expect(response.items[0].name).toBe('Amazon Card');
        expect(response.items[0].points).toBe(200);
      });

    const req = httpMock.expectOne((request) => request.url === REWARDS_URL);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('10');
    expect(req.request.params.get('search')).toBe('voucher');
    expect(req.request.params.get('status')).toBe('Active');
    expect(req.request.params.get('fromDate')).toBe('2026-09-01T00:00:00.000Z');
    expect(req.request.params.get('toDate')).toBe('2026-09-01T23:59:59.999Z');
    expect(req.request.params.get('sortBy')).toBe('points');
    expect(req.request.params.get('sortDescending')).toBe('true');

    req.flush({
      items: [
        {
          id: 7,
          name: 'Amazon Card',
          category: 'Gift Cards',
          points: 200,
          monetaryValue: 50,
          stockRatio: '2/3',
          redemptionCount: 4,
          isActive: true,
        },
      ],
      totalCount: 1,
      page: 2,
      pageSize: 10,
    });
  });

  it('should omit empty optional list filters', () => {
    service.getRewards({ page: 1, pageSize: 10 }).subscribe();

    const req = httpMock.expectOne((request) => request.url === REWARDS_URL);
    expect(req.request.params.get('search')).toBeNull();
    expect(req.request.params.get('status')).toBeNull();
    expect(req.request.params.get('fromDate')).toBeNull();
    req.flush({ items: [], totalCount: 0, page: 1, pageSize: 10 });
  });

  it('should POST create as multipart FormData to the real API', () => {
    const file = new File(['img'], 'banner.png', { type: 'image/png' });

    service
      .createReward({
        name: 'Amazon Card',
        description: 'Gift',
        categoryId: 1,
        points: 200,
        monetaryValue: 50,
        howToRedeem: 'Show code',
        termsOfUse: 'No cash',
        imageFile: file,
      })
      .subscribe((result) => {
        expect(result.id).toBe(12);
        expect(result.name).toBe('Amazon Card');
      });

    const req = httpMock.expectOne(REWARDS_URL);
    expect(req.request.method).toBe('POST');
    expect(req.request.body instanceof FormData).toBe(true);

    const body = req.request.body as FormData;
    expect(body.get('name')).toBe('Amazon Card');
    expect(body.get('categoryId')).toBe('1');
    expect(body.get('points')).toBe('200');
    expect(body.get('monetaryValue')).toBe('50');
    expect(body.get('howToRedeem')).toBe('Show code');
    expect(body.get('termsOfUse')).toBe('No cash');
    expect(body.get('imageFile')).toBeTruthy();

    req.flush(
      {
        id: 12,
        name: 'Amazon Card',
        description: 'Gift',
        imageUrl: null,
        category: 'Gift Cards',
        points: 200,
        monetaryValue: 50,
        howToRedeem: 'Show code',
        termsOfUse: 'No cash',
        isActive: true,
      },
      { status: 201, statusText: 'Created' },
    );
  });

  it('should PUT update as multipart FormData to /rewards/{id}', () => {
    const file = new File(['img'], 'banner.png', { type: 'image/png' });

    service
      .updateReward(12, {
        name: 'Amazon Card',
        description: 'Gift',
        categoryId: 1,
        points: 200,
        monetaryValue: 50,
        howToRedeem: 'Show code',
        termsOfUse: 'No cash',
        imageFile: file,
      })
      .subscribe((result) => {
        expect(result.id).toBe(12);
      });

    const req = httpMock.expectOne(`${REWARDS_URL}/12`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body instanceof FormData).toBe(true);
    const body = req.request.body as FormData;
    expect(body.get('name')).toBe('Amazon Card');
    expect(body.get('categoryId')).toBe('1');
    expect(body.get('imageFile')).toBeTruthy();
    req.flush({
      id: 12,
      name: 'Amazon Card',
      description: 'Gift',
      imageUrl: null,
      category: 'Gift Cards',
      points: 200,
      monetaryValue: 50,
      howToRedeem: 'Show code',
      termsOfUse: 'No cash',
      isActive: true,
    });
  });

  it('should DELETE a reward by id on the real API', () => {
    service.deleteReward(12).subscribe();

    const req = httpMock.expectOne(`${REWARDS_URL}/12`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });

  it('should GET reward profile by id from the real API', () => {
    service.getRewardProfile(12).subscribe((response) => {
      expect(response.profile.name).toBe('Amazon Card');
    });

    const req = httpMock.expectOne(`${REWARDS_URL}/12`);
    expect(req.request.method).toBe('GET');
    req.flush({
      profile: {
        id: 12,
        name: 'Amazon Card',
        description: 'Gift',
        imageUrl: null,
        category: 'Gift Cards',
        points: 200,
        monetaryValue: 50,
        howToRedeem: 'Show code',
        termsOfUse: 'No cash',
        isActive: true,
      },
      kpiStats: {
        redemptionCount: 0,
        availableStock: '0/0',
        totalCost: 0,
        topRedeemed: 0,
        pointsValue: 200,
      },
    });
  });

  it('should map GET reward by id from the real API profile', () => {
    service.getReward('8').subscribe((reward) => {
      expect(reward.id).toBe('8');
      expect(reward.name).toBe('Mock');
      expect(reward.status).toBe('Active');
    });

    const req = httpMock.expectOne(`${REWARDS_URL}/8`);
    expect(req.request.method).toBe('GET');
    req.flush({
      profile: {
        id: 8,
        name: 'Mock',
        description: '',
        imageUrl: null,
        category: 'Gift Cards',
        points: 1,
        monetaryValue: 1,
        howToRedeem: '',
        termsOfUse: '',
        isActive: true,
      },
      kpiStats: {
        redemptionCount: 0,
        availableStock: '0/0',
        totalCost: 0,
        topRedeemed: 0,
        pointsValue: 1,
      },
    });
  });
});
