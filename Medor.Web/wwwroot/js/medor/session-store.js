/**
 * Repository-style access to session-persisted UI state (localStorage).
 */
const PREFIX = 'medor.v1.';

/** Keys under the Medor v1 localStorage prefix (chart caches and live table history). */
export const SessionKeys = Object.freeze({
    /** Ordered list of live API snapshots (table + chart source of truth). */
    liveSamples: 'liveSamples',
    /** On /Live: keys of samples unchecked for DB save (see live-page.js). */
    liveSaveDeselected: 'liveSaveDeselected',
    /** @deprecated Prefer {@link SessionKeys.liveSamples}; read for one-time migration. */
    liveChart: 'liveChart',
    /** @deprecated Prefer {@link SessionKeys.liveSamples}; read for one-time migration. */
    liveSnapshot: 'liveSnapshot',
    savedChart: 'savedChart',
});

/** get/set/remove JSON values under the Medor `localStorage` prefix. */
export class SessionStore {
    /**
     * @param {string} key One of `SessionKeys` string values
     * @returns {any|null} Parsed JSON or null
     */
    get(key) {
        try {
            const s = localStorage.getItem(PREFIX + key);
            return s ? JSON.parse(s) : null;
        } catch {
            return null;
        }
    }

    /**
     * @param {string} key One of `SessionKeys` string values
     * @param {object} obj Serializable value
     */
    set(key, obj) {
        try {
            localStorage.setItem(PREFIX + key, JSON.stringify(obj));
        } catch {
            /* quota / private mode */
        }
    }

    /**
     * @param {string} key One of `SessionKeys` string values
     */
    remove(key) {
        try {
            localStorage.removeItem(PREFIX + key);
        } catch {
            /* */
        }
    }
}
