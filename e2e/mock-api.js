/**
 * Mocks Medor REST API (MedorApi:BaseUrl, default http://localhost:5298) so E2E needs no real API.
 * @param {import('@playwright/test').Page} page
 * @param {{ apiOrigin?: string }} [opts]
 */
export function setupMedorApiMocks(page, opts = {}) {
  const apiOrigin = (opts.apiOrigin ?? 'http://localhost:5298').replace(/\/$/, '');

  const livePrice = {
    btcEur: 95_234.56,
    btcCzk: 2_450_000.12,
    eurCzkRate: 25.1234,
    cnbRateValidFor: '2026-03-29',
    fetchedAtUtc: '2026-03-29T14:30:00.000Z',
  };

  const savedRows = [
    {
      id: 1,
      btcEur: 90_000,
      btcCzk: 2_000_000,
      eurCzkRate: 25,
      cnbRateValidFor: '2026-03-01',
      fetchedAtUtc: '2026-03-01T10:00:00.000Z',
      note: 'Alpha snapshot',
    },
    {
      id: 2,
      btcEur: 91_000,
      btcCzk: 2_050_000,
      eurCzkRate: 25.1,
      cnbRateValidFor: '2026-03-02',
      fetchedAtUtc: '2026-03-02T11:00:00.000Z',
      note: 'Beta snapshot',
    },
  ];

  const chartSeries = {
    labels: ['2026-03-01T10:00:00.0000000Z', '2026-03-02T11:00:00.0000000Z'],
    btcCzk: [2_000_000, 2_050_000],
    notes: ['Alpha snapshot', 'Beta snapshot'],
  };

  return page.route(`${apiOrigin}/api/**`, async (route) => {
    const req = route.request();
    const url = new URL(req.url());
    const path = url.pathname;
    const method = req.method();

    if (method === 'GET' && path.endsWith('/api/LivePrice')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(livePrice),
      });
    }

    if (method === 'GET' && path.endsWith('/api/SavedPrices/chart')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(chartSeries),
      });
    }

    if (method === 'GET' && path.endsWith('/api/SavedPrices')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(savedRows),
      });
    }

    if (method === 'POST' && path.endsWith('/api/SavedPrices')) {
      /** @type {unknown} */
      let payload = { id: 99, note: 'saved', ...livePrice };
      try {
        const posted = req.postDataJSON();
        if (posted && Array.isArray(posted.items) && posted.items.length) {
          payload = posted.items.map((row, i) => ({
            id: 99 + i,
            btcEur: row.btcEur,
            btcCzk: row.btcCzk,
            eurCzkRate: row.eurCzkRate,
            cnbRateValidFor: row.cnbRateValidFor,
            fetchedAtUtc: row.fetchedAtUtc,
            note: posted.note ?? 'saved',
          }));
        }
      } catch {
        /* ignore */
      }
      return route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify(payload),
      });
    }

    if (method === 'PUT' && path.endsWith('/api/SavedPrices/notes')) {
      return route.fulfill({ status: 204 });
    }

    if (method === 'DELETE' && path.endsWith('/api/SavedPrices')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ deleted: 1 }),
      });
    }

    return route.fulfill({ status: 404, body: 'e2e mock: unknown ' + path });
  });
}
