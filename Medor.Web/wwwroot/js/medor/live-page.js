import { SessionStore, SessionKeys } from './session-store.js';
import { MedorApiClient } from './api-client.js';

/** Up arrow (BTC/CZK vs older sample); same path rotated for down. */
const ARROW_UP_SVG =
    '<svg class="live-arrow-icon" xmlns="http://www.w3.org/2000/svg" width="18" height="18" fill="currentColor" viewBox="0 0 16 16" aria-hidden="true">' +
    '<path fill-rule="evenodd" d="M8 15a.5.5 0 0 0 .5-.5V2.707l3.146 3.147a.5.5 0 0 0 .708-.708l-4-4a.5.5 0 0 0-.708 0l-4 4a.5.5 0 1 0 .708.708L7.5 2.707V14.5a.5.5 0 0 0 .5.5z"/>' +
    '</svg>';

/**
 * Live price table (accumulated poll history), polling, Chart.js series, and session persistence.
 */
export class LivePage {
    /**
     * @param {{ api: object, store: object }} options API client and session store (`MedorApiClient`, `SessionStore`).
     */
    constructor(options) {
        this._api = options.api;
        this._store = options.store;
        /** @type {object[]} */
        this._samples = [];
        this._labels = [];
        this._points = [];
        this._maxPoints = 40;
        this._chart = null;
        this._pollIntervalMs = 30000;
        this._countdownAnim = null;
        this._countdownIntervalId = null;
        /** @type {Set<string>} Stable keys of samples the user unchecked for DB save */
        this._deselectedKeys = new Set();
    }

    /** Stable key for a sample row (persisted selection / prune after trim). */
    _sampleKey(d) {
        return String(d.fetchedAtUtc) + '|' + Number(d.btcCzk);
    }

    /** Drops deselection entries that no longer exist in {@link LivePage#_samples}. */
    _pruneDeselectionKeys() {
        const valid = new Set(this._samples.map((x) => this._sampleKey(x)));
        for (const k of this._deselectedKeys) {
            if (!valid.has(k)) this._deselectedKeys.delete(k);
        }
    }

    /** Wires buttons, restores session chart/snapshot, starts polling and first refresh. */
    init() {
        this._restoreFromSession();
        if (this._labels.length) {
            this._ensureChart();
            this._chart.update();
        }
        this._wireTableSelection();
        this._refresh();
        setInterval(() => this._refresh(), this._pollIntervalMs);

        document.getElementById('btn-save').addEventListener('click', () => this._onSaveClick());
        document.getElementById('btn-reset').addEventListener('click', () => this._onResetClick());
    }

    /** Checkbox column: row selection and “select all” (delegated on `#live-table`). */
    _wireTableSelection() {
        const table = document.getElementById('live-table');
        if (!table) return;
        table.addEventListener('change', (e) => {
            const t = e.target;
            if (t.id === 'live-chk-all') {
                if (t.checked) {
                    this._deselectedKeys.clear();
                } else {
                    this._samples.forEach((d) => this._deselectedKeys.add(this._sampleKey(d)));
                }
                this._persistSession();
                this._renderTable(this._samples);
                return;
            }
            if (!t.classList?.contains('live-row-chk')) return;
            const raw = t.getAttribute('data-save-key');
            const key = raw ? decodeURIComponent(raw) : '';
            if (!key) return;
            if (t.checked) this._deselectedKeys.delete(key);
            else this._deselectedKeys.add(key);
            this._persistSession();
            this._syncSelectAllCheckbox();
        });
    }

    /** Syncs header “select all” checked / indeterminate from {@link LivePage#_deselectedKeys}. */
    _syncSelectAllCheckbox() {
        const el = document.getElementById('live-chk-all');
        if (!el || !this._samples.length) return;
        let selected = 0;
        for (const d of this._samples) {
            if (!this._deselectedKeys.has(this._sampleKey(d))) selected++;
        }
        const n = this._samples.length;
        el.checked = selected === n;
        el.indeterminate = selected > 0 && selected < n;
    }

    /**
     * Hollow ring empties over {@link LivePage#_pollIntervalMs}; restarts after each successful fetch.
     */
    _startPollCountdown() {
        const el = document.getElementById('live-countdown-progress');
        if (!el) return;
        const r = parseFloat(el.getAttribute('r')) || 14;
        const c = 2 * Math.PI * r;
        el.style.strokeDasharray = String(c);
        el.style.strokeDashoffset = '0';

        if (this._countdownAnim) {
            try {
                this._countdownAnim.cancel();
            } catch {
                /* ignore */
            }
            this._countdownAnim = null;
        }
        if (this._countdownIntervalId !== null) {
            clearInterval(this._countdownIntervalId);
            this._countdownIntervalId = null;
        }

        const reduced = typeof window.matchMedia === 'function' && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        const secTotal = Math.round(this._pollIntervalMs / 1000);
        if (reduced) {
            let sec = secTotal;
            this._countdownIntervalId = window.setInterval(() => {
                sec -= 1;
                el.style.strokeDashoffset = String((c * (secTotal - sec)) / secTotal);
                if (sec <= 0) {
                    clearInterval(this._countdownIntervalId);
                    this._countdownIntervalId = null;
                }
            }, 1000);
            return;
        }

        this._countdownAnim = el.animate(
            [{ strokeDashoffset: 0 }, { strokeDashoffset: c }],
            { duration: this._pollIntervalMs, easing: 'linear', fill: 'forwards' },
        );
    }

    /** Loads samples from session (or legacy keys), rebuilds chart series and table. */
    _restoreFromSession() {
        let raw = this._store.get(SessionKeys.liveSamples);
        if (!raw?.length) {
            const snap = this._store.get(SessionKeys.liveSnapshot);
            if (snap && typeof snap.btcEur !== 'undefined') {
                raw = [snap];
            }
        }
        if (!raw?.length) return;

        this._samples = raw.length > this._maxPoints ? raw.slice(-this._maxPoints) : raw.slice();
        const des = this._store.get(SessionKeys.liveSaveDeselected);
        this._deselectedKeys = Array.isArray(des) && des.length ? new Set(des) : new Set();
        this._pruneDeselectionKeys();
        this._syncChartFromSamples();
        this._renderTable(this._samples);
    }

    /** Persists current sample history and save-selection state to session storage. */
    _persistSession() {
        this._store.set(SessionKeys.liveSamples, this._samples.slice());
        this._store.set(SessionKeys.liveSaveDeselected, Array.from(this._deselectedKeys));
    }

    /** Rebuilds chart arrays from {@link LivePage#_samples} (restore, reset). */
    _syncChartFromSamples() {
        this._labels.length = 0;
        this._points.length = 0;
        this._samples.forEach((d) => {
            const t = new Date(d.fetchedAtUtc).toLocaleTimeString('cs-CZ');
            this._labels.push(t);
            this._points.push(Number(d.btcCzk));
        });
    }

    /**
     * Appends one poll point and trims to {@link LivePage#_maxPoints} (must match {@link LivePage#_samples} push/shift).
     * @param {object} d Latest live API row
     */
    _appendChartPointForLatestSample(d) {
        const t = new Date(d.fetchedAtUtc).toLocaleTimeString('cs-CZ');
        this._labels.push(t);
        this._points.push(Number(d.btcCzk));
        while (this._labels.length > this._maxPoints) {
            this._labels.shift();
            this._points.shift();
        }
    }

    /** Updates Chart.js without re-animating the whole series (avoids “reload” on each poll). */
    _updateChartQuiet() {
        if (this._chart) {
            this._chart.update('none');
        }
    }

    /**
     * Compares {@code curr} to chronologically older {@code older}; green up / red down / muted dash for flat.
     * @param {object} curr
     * @param {object|null} older Previous sample in time, or null for oldest row
     * @returns {string}
     */
    _directionArrowHtml(curr, older) {
        if (older == null) {
            return '<span class="text-muted" title="Nejstarší vzorek — žádné srovnání">—</span>';
        }
        const a = Number(curr.btcCzk);
        const b = Number(older.btcCzk);
        const eps = 1e-4;
        if (Math.abs(a - b) < eps) {
            return '<span class="text-muted" title="Beze změny oproti předchozímu vzorku (BTC/CZK)">—</span>';
        }
        if (a > b) {
            return (
                '<span class="text-success d-inline-flex align-items-center" title="BTC/CZK vzrostlo oproti předchozímu vzorku">' +
                ARROW_UP_SVG +
                '</span>'
            );
        }
        return (
            '<span class="text-danger d-inline-flex align-items-center" title="BTC/CZK kleslo oproti předchozímu vzorku">' +
            '<span class="live-arrow-down">' +
            ARROW_UP_SVG +
            '</span></span>'
        );
    }

    /**
     * @param {object[]} samples Chronological order: oldest → newest (chart order). Table shows newest first.
     * @param {{ animateNewestArrow?: boolean }} [opts] If true, briefly animates the arrow in the top (newest) row (poll refresh).
     */
    _renderTable(samples, opts) {
        const animateNewest = !!(opts && opts.animateNewestArrow);
        const tbody = document.querySelector('#live-table tbody');
        if (!tbody) return;

        if (!samples.length) {
            tbody.innerHTML =
                '<tr><td colspan="7" class="text-muted" id="live-status">Zatím žádná načtená data.</td></tr>';
            return;
        }

        const n = samples.length;
        const rowsHtml = [];
        for (let displayIdx = 0; displayIdx < n; displayIdx++) {
            const chronoIdx = n - 1 - displayIdx;
            const d = samples[chronoIdx];
            const key = this._sampleKey(d);
            const rowChecked = !this._deselectedKeys.has(key);
            const keyAttr = encodeURIComponent(key);
            const older = chronoIdx > 0 ? samples[chronoIdx - 1] : null;
            let arrowInner = this._directionArrowHtml(d, older);
            if (animateNewest && displayIdx === 0) {
                arrowInner = '<span class="live-arrow-pop">' + arrowInner + '</span>';
            }
            const cnbStr = d.cnbRateValidFor != null ? String(d.cnbRateValidFor) : '';
            rowsHtml.push(
                '<tr class="live-rate-row">' +
                    '<td class="live-table-chk-cell">' +
                    '<input type="checkbox" class="live-row-chk" data-save-key="' +
                    keyAttr +
                    '" ' +
                    (rowChecked ? 'checked ' : '') +
                    'aria-label="Uložit tento vzorek do databáze" />' +
                    '</td>' +
                    '<td class="live-arrow-cell">' +
                    '<span class="live-arrow-slot">' +
                    arrowInner +
                    '</span>' +
                    '</td>' +
                    '<td>' +
                    Number(d.btcEur).toLocaleString('cs-CZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) +
                    '</td>' +
                    '<td>' +
                    Number(d.eurCzkRate).toLocaleString('cs-CZ', { minimumFractionDigits: 4, maximumFractionDigits: 4 }) +
                    '</td>' +
                    '<td class="text-nowrap"><strong>' +
                    Number(d.btcCzk).toLocaleString('cs-CZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) +
                    '</strong></td>' +
                    '<td>' +
                    cnbStr +
                    '</td>' +
                    '<td>' +
                    d.fetchedAtUtc +
                    '</td>' +
                    '</tr>',
            );
        }
        tbody.innerHTML = rowsHtml.join('');
        this._syncSelectAllCheckbox();
    }

    /** Lazily creates the Chart.js line chart for BTC/CZK if missing. */
    _ensureChart() {
        const ChartCtor = window.Chart;
        if (!ChartCtor) throw new Error('Chart.js is not loaded');
        if (this._chart) return;
        const ctx = document.getElementById('live-chart');
        this._chart = new ChartCtor(ctx, {
            type: 'line',
            data: {
                labels: this._labels,
                datasets: [
                    {
                        label: 'BTC/CZK',
                        data: this._points,
                        borderColor: 'rgb(13, 110, 253)',
                        tension: 0.2,
                        fill: false,
                    },
                ],
            },
            options: {
                responsive: true,
                scales: {
                    y: { beginAtZero: false },
                },
            },
        });
    }

    /** Fetches live price, appends to history, updates table/chart, session, and poll countdown. */
    async _refresh() {
        const status = document.getElementById('live-status');
        try {
            const d = await this._api.getLivePrice();
            this._samples.push(d);
            while (this._samples.length > this._maxPoints) {
                this._samples.shift();
            }
            this._pruneDeselectionKeys();
            this._appendChartPointForLatestSample(d);
            this._renderTable(this._samples, { animateNewestArrow: true });
            this._ensureChart();
            this._updateChartQuiet();
            this._persistSession();
            this._startPollCountdown();
        } catch {
            if (status) {
                status.textContent =
                    'Nepodařilo se načíst data. Je spuštěné API (Medor.Api) a správná MedorApi:BaseUrl?';
            } else if (this._samples.length === 0) {
                const tbody = document.querySelector('#live-table tbody');
                if (tbody) {
                    tbody.innerHTML =
                        '<tr><td colspan="7" class="text-danger">Nepodařilo se načíst data. Je spuštěné API (Medor.Api) a správná MedorApi:BaseUrl?</td></tr>';
                }
            }
        }
    }

    /**
     * Maps client samples to API batch items (camelCase JSON).
     * @param {object[]} samples
     * @returns {object[]}
     */
    _toApiItems(samples) {
        return samples.map((d) => {
            let fetched = d.fetchedAtUtc;
            if (fetched instanceof Date) {
                fetched = fetched.toISOString();
            }
            let cnb = d.cnbRateValidFor;
            if (cnb instanceof Date) {
                cnb = cnb.toISOString().slice(0, 10);
            }
            return {
                btcEur: Number(d.btcEur),
                btcCzk: Number(d.btcCzk),
                eurCzkRate: Number(d.eurCzkRate),
                cnbRateValidFor: cnb,
                fetchedAtUtc: fetched,
            };
        });
    }

    /** Validates note and POSTs all table rows as one batch. */
    async _onSaveClick() {
        const noteElement = document.getElementById('save-note');
        const note = noteElement.value.trim();
        const err = document.getElementById('save-error');
        const ok = document.getElementById('save-ok');
        err.style.display = 'none';
        ok.style.display = 'none';
        if (!note) {
            err.textContent = 'Vyplňte poznámku.';
            err.style.display = 'inline';
            return;
        }
        if (!this._samples.length) {
            err.textContent = 'V tabulce nejsou žádná načtená data k uložení.';
            err.style.display = 'inline';
            return;
        }
        const toSave = this._samples.filter((d) => !this._deselectedKeys.has(this._sampleKey(d)));
        if (!toSave.length) {
            err.textContent = 'Vyberte alespoň jeden řádek k uložení (zaškrtněte vzorky v prvním sloupci).';
            err.style.display = 'inline';
            return;
        }
        try {
            const result = await this._api.savePrices(note, this._toApiItems(toSave));
            const n = Array.isArray(result) ? result.length : 1;
            ok.textContent = n === 1 ? 'Uloženo 1 záznam.' : `Uloženo ${n} záznamů.`;
            ok.style.display = 'inline';
        } catch (e) {
            err.textContent = e.message || 'Uložení se nezdařilo.';
            err.style.display = 'inline';
        }
        noteElement.value = '';
    }

    /** Clears cached samples, reloads one live point and chart. */
    async _onResetClick() {
        this._store.remove(SessionKeys.liveSamples);
        this._store.remove(SessionKeys.liveChart);
        this._store.remove(SessionKeys.liveSnapshot);
        this._store.remove(SessionKeys.savedChart);
        this._store.remove(SessionKeys.liveSaveDeselected);
        this._deselectedKeys.clear();
        this._samples.length = 0;
        this._labels.length = 0;
        this._points.length = 0;
        try {
            const d = await this._api.getLivePrice();
            this._samples.push(d);
            this._syncChartFromSamples();
            this._renderTable(this._samples);
            this._ensureChart();
            this._updateChartQuiet();
            this._persistSession();
            this._startPollCountdown();
        } catch {
            const tbody = document.querySelector('#live-table tbody');
            if (tbody) {
                tbody.innerHTML =
                    '<tr><td colspan="7" class="text-danger">Nepodařilo se načíst aktuální kurz po resetu.</td></tr>';
            }
        }
    }
}

const apiBase = window.MedorApiBase || '';
const page = new LivePage({
    api: new MedorApiClient(apiBase),
    store: new SessionStore(),
});
page.init();
