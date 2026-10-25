import { DEFAULT_PAGE_SIZE, mapPage, MAX_PAGE_SIZE, pageRequest, totalPages } from './page';

describe('page', () => {
  it('builds clamped page requests', () => {
    expect(pageRequest()).toEqual({ page: 1, pageSize: DEFAULT_PAGE_SIZE });
    expect(pageRequest(0, 0)).toEqual({ page: 1, pageSize: 1 });
    expect(pageRequest(2.7, 500)).toEqual({ page: 2, pageSize: MAX_PAGE_SIZE });
  });

  it('counts pages and maps items', () => {
    expect(totalPages({ pageSize: 20, totalItems: 0 })).toBe(1);
    expect(totalPages({ pageSize: 20, totalItems: 41 })).toBe(3);
    expect(
      mapPage({ items: [1, 2], page: 1, pageSize: 2, totalItems: 5 }, (item) => item * 10),
    ).toEqual({
      items: [10, 20],
      page: 1,
      pageSize: 2,
      totalItems: 5,
    });
  });
});
