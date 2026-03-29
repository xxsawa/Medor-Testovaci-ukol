import { test, expect } from '@playwright/test';
import { setupMedorApiMocks } from '../mock-api.js';

test.describe('Medor assignment (mocked API)', () => {
  test.beforeEach(async ({ page }) => {
    await setupMedorApiMocks(page);
  });

  test('home redirects to Live', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveURL(/\/Live\/?$/i);
    await expect(page.getByRole('heading', { name: /Aktuální kurz/i })).toBeVisible();

    await new Promise((resolve) => setTimeout(resolve, 10000));
  });

  test('Live: table and chart show mocked BTC data', async ({ page }) => {
    await page.goto('/Live');
    await expect(page.locator('#live-table')).toContainText('2 450 000');
    await expect(page.locator('#live-chart')).toBeVisible();
    await expect(page.locator('.live-countdown')).toBeVisible();

    await new Promise((resolve) => setTimeout(resolve, 10000));
  });

  test('Live: save selected rows with note (batch POST)', async ({ page }) => {
    const post = page.waitForRequest(
      (r) =>
        r.method() === 'POST' &&
        r.url().includes('/api/SavedPrices') &&
        !r.url().includes('notes'),
    );

    await page.goto('/Live');
    await expect(page.locator('#live-table tbody tr').first()).toBeVisible({ timeout: 20_000 });

    await page.locator('#save-note').fill('Batch from e2e');
    await page.locator('#btn-save').click();

    const req = await post;
    const body = req.postDataJSON();
    expect(body.note).toBe('Batch from e2e');
    expect(Array.isArray(body.items)).toBeTruthy();
    expect(body.items.length).toBeGreaterThanOrEqual(1);

    await expect(page.locator('#save-ok')).toBeVisible();

    await new Promise((resolve) => setTimeout(resolve, 10000));
  });

  test('Live: unchecking all rows blocks save (validation)', async ({ page }) => {
    await page.goto('/Live');
    await expect(page.locator('#live-table tbody tr.live-rate-row').first()).toBeVisible({ timeout: 20_000 });
    await page.locator('#live-table tbody tr.live-rate-row .live-row-chk').uncheck();
    await page.locator('#save-note').fill('Should not post');
    let posted = false;
    page.on('request', (r) => {
      if (r.method() === 'POST' && r.url().includes('/api/SavedPrices')) posted = true;
    });
    await page.locator('#btn-save').click();
    await expect(page.locator('#save-error')).toContainText(/alespoň jeden|Vyberte/i);
    expect(posted).toBe(false);

    await new Promise((resolve) => setTimeout(resolve, 10000));
  });

  test('Saved: table, DataTables search, chart, unsaved banner, save + delete with confirm', async ({
    page,
  }) => {
    await page.goto('/Saved');
    await expect(page.locator('#saved-table')).toContainText('Alpha snapshot');
    await expect(page.locator('#saved-table')).toContainText('Beta snapshot');
    await expect(page.locator('#saved-chart')).toBeVisible();

    const search = page
      .locator('#saved-table_wrapper .dt-search input')
      .or(page.locator('#saved-table_wrapper input[type="search"]'))
      .or(page.locator('#saved-table_wrapper .dataTables_filter input'))
      .first();
    await search.fill('Beta');
    await expect(page.locator('#saved-table tbody tr:visible')).toHaveCount(1);
    await expect(page.locator('#saved-table tbody')).toContainText('Beta snapshot');
    await search.fill('');

    const firstNote = page.locator('#saved-table tbody tr .note-input').first();
    await firstNote.fill('Alpha snapshot edited');
    await expect(page.locator('#saved-unsaved')).toBeVisible();

    const put = page.waitForRequest(
      (r) => r.method() === 'PUT' && r.url().includes('/api/SavedPrices/notes'),
    );
    page.once('dialog', (d) => {
      expect(d.message()).toMatch(/uložit|Uložit|poznámek/i);
      void d.accept();
    });
    await page.locator('#btn-save-notes').click();
    await put;

    await page.locator('#saved-table tbody tr').first().locator('.row-chk').check();

    const del = page.waitForRequest((r) => r.method() === 'DELETE' && r.url().includes('/api/SavedPrices'));
    page.once('dialog', (d) => {
      expect(d.message()).toMatch(/smazat|Smazat/i);
      void d.accept();
    });
    await page.locator('#btn-delete').click();
    await del;

    await new Promise((resolve) => setTimeout(resolve, 10000));
  });
});
