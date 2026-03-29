import { SessionStore, SessionKeys } from './session-store.js';
import { MedorApiClient } from './api-client.js';

/**
 * @param {string} s Raw text for HTML attribute or text node
 * @returns {string} Escaped string safe for insertion into HTML
 */
function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, (c) =>
        ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c],
    );
}

/**
 * Short single-line x-axis text for the saved chart (full text stays in tooltips).
 * @param {string} full Label from API (localized date/time string).
 * @returns {string}
 */
function compactSavedChartAxisLabel(full) {
    const s = String(full ?? '').trim();
    if (!s) return '';
    const parsed = Date.parse(s);
    if (!Number.isNaN(parsed)) {
        return new Date(parsed).toLocaleString('cs-CZ', {
            day: 'numeric',
            month: 'numeric',
            hour: '2-digit',
            minute: '2-digit',
        });
    }
    const oneLine = s.split('\n')[0].trim();
    return oneLine.length > 18 ? oneLine.slice(0, 16) + '…' : oneLine;
}

/**
 * Saved records DataTable, chart from API, and cached chart in session storage.
 */
export class SavedPage {
    /**
     * @param {{ api: object, store: object }} options API client and `SessionStore`
     */
    constructor(options) {
        this._api = options.api;
        this._store = options.store;
        this._chart = null;
        this._table = null;
        /** @type {Map<number, string>} Last saved note text per row id (for dirty detection) */
        this._savedNoteBaseline = new Map();
    }

    /** Loads table and chart, wires toolbar handlers. */
    async init() {
        const cached = this._store.get(SessionKeys.savedChart);
        if (cached && cached.labels && cached.labels.length && cached.btcCzk && cached.btcCzk.length) {
            this._paintChart({
                labels: cached.labels,
                btcCzk: cached.btcCzk,
                notes: cached.notes || [],
            });
        }

        try {
            const rows = await this._api.getSavedPrices();
            this._renderTable(rows);
            this._table = new DataTable('#saved-table', {
                order: [[1, 'desc']],
                language: {
                    url: 'https://cdn.datatables.net/plug-ins/2.1.8/i18n/cs.json',
                },
                columnDefs: [
                    { orderable: false, searchable: false, targets: 0 },
                    { orderable: false, targets: 7 },
                ],
            });
            this._wireNoteSearchSync();
            await this._refreshChart();
        } catch {
            const el = document.getElementById('saved-error');
            el.textContent = 'Nelze načíst data z API.';
            el.style.display = 'inline';
        }

        document.getElementById('chk-all').addEventListener('change', () => this._onChkAllChange());
        document.getElementById('btn-delete').addEventListener('click', () => this._onDeleteClick());
        document.getElementById('btn-save-notes').addEventListener('click', () => this._onSaveNotesClick());
    }

    /**
     * @param {object[]} rows Saved price rows from API
     */
    _renderTable(rows) {
        const tbody = document.querySelector('#saved-table tbody');
        tbody.innerHTML = '';
        rows.forEach((row) => {
            const tr = document.createElement('tr');
            tr.dataset.id = String(row.id);
            tr.innerHTML =
                '<td><input type="checkbox" class="row-chk" data-id="' +
                row.id +
                '" /></td>' +
                '<td>' +
                row.id +
                '</td>' +
                '<td>' +
                Number(row.btcEur).toLocaleString('cs-CZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) +
                '</td>' +
                '<td>' +
                Number(row.eurCzkRate).toLocaleString('cs-CZ', { minimumFractionDigits: 4, maximumFractionDigits: 4 }) +
                '</td>' +
                '<td>' +
                Number(row.btcCzk).toLocaleString('cs-CZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) +
                '</td>' +
                '<td>' +
                row.cnbRateValidFor +
                '</td>' +
                '<td>' +
                row.fetchedAtUtc +
                '</td>' +
                '<td class="align-middle">' +
                '<span class="visually-hidden note-for-search">' +
                escapeHtml(row.note) +
                '</span>' +
                '<input type="text" class="form-control form-control-sm note-input" value="' +
                escapeHtml(row.note) +
                '" maxlength="500" aria-label="Poznámka" />' +
                '</td>';
            tbody.appendChild(tr);
        });
        this._savedNoteBaseline = new Map();
        rows.forEach((row) => {
            this._savedNoteBaseline.set(row.id, String(row.note ?? '').trim());
        });
        this._updateDirtyBanner();
    }

    /** Shows or hides the “unsaved changes” banner by comparing inputs to the last saved baseline. */
    _updateDirtyBanner() {
        const el = document.getElementById('saved-unsaved');
        if (!el) return;
        let dirty = false;
        document.querySelectorAll('#saved-table tbody tr').forEach((tr) => {
            const id = parseInt(tr.dataset.id, 10);
            const input = tr.querySelector('.note-input');
            if (!input) return;
            const cur = input.value.trim();
            const base = this._savedNoteBaseline.has(id) ? this._savedNoteBaseline.get(id) : cur;
            if (cur !== base) dirty = true;
        });
        el.style.display = dirty ? 'block' : 'none';
    }

    /**
     * @param {{ labels?: string[], btcCzk?: number[], notes?: string[] }} s Chart series payload (notes align with labels)
     */
    _paintChart(s) {
        const ChartCtor = window.Chart;
        if (!ChartCtor) return;
        const ctx = document.getElementById('saved-chart');
        if (this._chart) this._chart.destroy();

        const timeLabels = s.labels || [];
        const notes = s.notes || [];
        const axisLabels = timeLabels.map((lab) => compactSavedChartAxisLabel(lab));

        this._chart = new ChartCtor(ctx, {
            type: 'line',
            data: {
                labels: axisLabels,
                datasets: [
                    {
                        label: 'BTC/CZK',
                        data: (s.btcCzk || []).map(Number),
                        borderColor: 'rgb(25, 135, 84)',
                        tension: 0.2,
                        fill: false,
                    },
                ],
            },
            options: {
                responsive: true,
                layout: {
                    padding: { bottom: 4 },
                },
                scales: {
                    x: {
                        grid: {
                            display: true,
                            drawTicks: true,
                        },
                        ticks: {
                            autoSkip: true,
                            maxTicksLimit: 12,
                            maxRotation: 35,
                            minRotation: 0,
                            font: { size: 10 },
                            color: '#6c757d',
                            padding: 6,
                        },
                    },
                    y: {
                        beginAtZero: false,
                        ticks: {
                            font: { size: 10 },
                            color: '#6c757d',
                            callback: (v) =>
                                typeof v === 'number'
                                    ? v.toLocaleString('cs-CZ', {
                                          maximumFractionDigits: 0,
                                      })
                                    : v,
                        },
                    },
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            title(tooltipItems) {
                                const i = tooltipItems[0]?.dataIndex;
                                const raw = i != null ? timeLabels[i] : null;
                                if (raw == null) return '';
                                const p = Date.parse(String(raw));
                                if (!Number.isNaN(p)) {
                                    return new Date(p).toLocaleString('cs-CZ', {
                                        dateStyle: 'short',
                                        timeStyle: 'medium',
                                    });
                                }
                                return String(raw);
                            },
                            label(ctx) {
                                const v = ctx.parsed.y;
                                if (v == null) return '';
                                return (
                                    'BTC/CZK: ' +
                                    Number(v).toLocaleString('cs-CZ', {
                                        minimumFractionDigits: 2,
                                        maximumFractionDigits: 2,
                                    })
                                );
                            },
                            afterLabel(ctx) {
                                const i = ctx.dataIndex;
                                const n = notes[i];
                                if (n == null || String(n).trim() === '') return '';
                                return 'Poznámka: ' + String(n);
                            },
                        },
                    },
                },
            },
        });
    }

    /** Fetches chart series from API, updates cache and canvas. */
    async _refreshChart() {
        const s = await this._api.getSavedChartSeries();
        this._store.set(SessionKeys.savedChart, {
            labels: s.labels || [],
            btcCzk: s.btcCzk || [],
            notes: s.notes || [],
        });
        this._paintChart(s);
    }

    /**
     * Keeps hidden note text in sync for DataTables global search (inputs are not searched by default).
     * Avoids invalidate+draw on every input — that redraws cells and drops focus (paste/typing).
     * After focus leaves notes, refreshes the search index when no note field is focused.
     */
    _wireNoteSearchSync() {
        const tableEl = document.getElementById('saved-table');
        if (!tableEl || !this._table) return;
        const tbody = tableEl.querySelector('tbody');
        if (!tbody) return;

        const refreshSearchWhenNotesBlurred = () => {
            window.setTimeout(() => {
                if (tableEl.querySelector('.note-input:focus')) return;
                this._table.rows().invalidate('dom').draw(false);
            }, 0);
        };

        tbody.addEventListener('input', (e) => {
            const input = e.target;
            if (!input.classList?.contains('note-input')) return;
            const span = input.closest('tr')?.querySelector('.note-for-search');
            if (span) span.textContent = input.value;
            this._updateDirtyBanner();
        });

        tbody.addEventListener('focusout', (e) => {
            if (!e.target.classList?.contains('note-input')) return;
            refreshSearchWhenNotesBlurred();
        });
    }

    /** Toggles all row checkboxes from the header checkbox. */
    _onChkAllChange() {
        const checked = document.getElementById('chk-all').checked;
        document.querySelectorAll('.row-chk').forEach((c) => {
            c.checked = checked;
        });
    }

    /** Deletes selected rows after confirm; reloads page on success. */
    async _onDeleteClick() {
        const ids = Array.from(document.querySelectorAll('.row-chk:checked')).map((c) => parseInt(c.dataset.id, 10));
        const err = document.getElementById('saved-error');
        const ok = document.getElementById('saved-ok');
        err.style.display = 'none';
        ok.style.display = 'none';
        if (!ids.length) {
            err.textContent = 'Není nic vybráno.';
            err.style.display = 'inline';
            return;
        }
        if (
            !window.confirm(
                'Opravdu chcete smazat vybrané záznamy? Tuto akci nelze vrátit zpět.',
            )
        ) {
            return;
        }
        try {
            await this._api.deleteSavedPrices(ids);
            location.reload();
        } catch {
            err.textContent = 'Mazání se nezdařilo.';
            err.style.display = 'inline';
        }
    }

    /** Validates all notes non-empty and PUTs bulk update. */
    async _onSaveNotesClick() {
        const err = document.getElementById('saved-error');
        const ok = document.getElementById('saved-ok');
        err.style.display = 'none';
        ok.style.display = 'none';
        const items = [];
        let missingNote = false;
        document.querySelectorAll('#saved-table tbody tr').forEach((tr) => {
            const id = parseInt(tr.dataset.id, 10);
            const note = tr.querySelector('.note-input').value.trim();
            if (!note) missingNote = true;
            items.push({ id, note });
        });
        if (!items.length) {
            err.textContent = 'Žádné řádky.';
            err.style.display = 'inline';
            return;
        }
        if (missingNote) {
            err.textContent = 'Každá poznámka musí být vyplněna.';
            err.style.display = 'inline';
            return;
        }
        if (
            !window.confirm(
                'Opravdu uložit upravené poznámky do databáze? Graf se aktualizuje o nové texty poznámek.',
            )
        ) {
            return;
        }
        try {
            await this._api.updateSavedNotes({ items });
            document.querySelectorAll('#saved-table tbody tr').forEach((tr) => {
                const id = parseInt(tr.dataset.id, 10);
                const note = tr.querySelector('.note-input').value.trim();
                this._savedNoteBaseline.set(id, note);
            });
            await this._refreshChart();
            ok.style.display = 'inline';
            this._updateDirtyBanner();
        } catch (e) {
            err.textContent = e.message || 'Uložení se nezdařilo.';
            err.style.display = 'inline';
        }
    }
}

const apiBase = window.MedorApiBase || '';
const saved = new SavedPage({
    api: new MedorApiClient(apiBase),
    store: new SessionStore(),
});
saved.init().catch((e) => console.error(e));
