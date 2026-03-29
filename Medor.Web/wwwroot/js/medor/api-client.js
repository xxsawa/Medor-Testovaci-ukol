/**
 * Facade for Medor REST API calls used by the web UI.
 */
export class MedorApiClient {
    /**
     * @param {string} [baseUrl] API origin (no trailing slash), e.g. `window.MedorApiBase`.
     */
    constructor(baseUrl) {
        this._base = (baseUrl || '').replace(/\/$/, '');
    }

    /**
     * @param {string} path Absolute path beginning with `/api/...`
     * @returns {string}
     */
    _url(path) {
        return this._base + path;
    }

    /**
     * @param {string} path
     * @returns {Promise<any>}
     */
    async getJson(path) {
        const r = await fetch(this._url(path), { credentials: 'omit' });
        if (!r.ok) throw new Error('HTTP ' + r.status);
        return r.json();
    }

    /**
     * @param {string} path
     * @param {object} body JSON-serializable body
     * @returns {Promise<any>}
     */
    async postJson(path, body) {
        const r = await fetch(this._url(path), {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
            credentials: 'omit',
        });
        if (!r.ok) {
            const j = await r.json().catch(() => ({}));
            throw new Error(j.error || 'HTTP ' + r.status);
        }
        return r.json().catch(() => ({}));
    }

    /**
     * @param {string} path
     * @param {object} body
     * @returns {Promise<any>}
     */
    async putJson(path, body) {
        const r = await fetch(this._url(path), {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
            credentials: 'omit',
        });
        if (!r.ok) {
            const j = await r.json().catch(() => ({}));
            throw new Error(j.error || 'HTTP ' + r.status);
        }
        return r.json().catch(() => ({}));
    }

    /**
     * @param {string} path
     * @param {object} body
     * @returns {Promise<any>}
     */
    async deleteJson(path, body) {
        const r = await fetch(this._url(path), {
            method: 'DELETE',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body),
            credentials: 'omit',
        });
        if (!r.ok) throw new Error('HTTP ' + r.status);
        return r.json().catch(() => ({}));
    }

    /** @returns {Promise<object>} Live price DTO */
    getLivePrice() {
        return this.getJson('/api/LivePrice');
    }

    /** @returns {Promise<object[]>} Saved rows */
    getSavedPrices() {
        return this.getJson('/api/SavedPrices');
    }

    /** @returns {Promise<{ labels: string[], btcCzk: number[] }>} Chart series */
    getSavedChartSeries() {
        return this.getJson('/api/SavedPrices/chart');
    }

    /**
     * Saves either the current server-side live snapshot ({@code note} only) or a batch of observed rows ({@code items}).
     *
     * @param {string} note Required note (applied to each row when {@code items} is set)
     * @param {object[]|undefined} items Optional snapshots matching the live price DTO shape
     * @returns {Promise<object|object[]>} Single saved row or array when batch
     */
    savePrices(note, items) {
        const body = items?.length ? { note, items } : { note };
        return this.postJson('/api/SavedPrices', body);
    }

    /**
     * @param {number[]} ids Row ids to delete
     * @returns {Promise<object>}
     */
    deleteSavedPrices(ids) {
        return this.deleteJson('/api/SavedPrices', { ids });
    }

    /**
     * @param {{ items: { id: number, note: string }[] }} body Bulk note update payload
     * @returns {Promise<any>}
     */
    updateSavedNotes(body) {
        return this.putJson('/api/SavedPrices/notes', body);
    }
}
