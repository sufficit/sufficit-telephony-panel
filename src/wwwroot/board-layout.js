const storageKey = 'telephony-panel-board-layout-v1';
const defaults = { columns: 0.76, rows: null };
const clamp = (value, min, max) => Math.min(max, Math.max(min, value));

export function attach(root) {
    const sidebar = root.querySelector('.board-sidebar');
    const handles = [...root.querySelectorAll('[data-board-split]')];
    const desktop = matchMedia('(min-width: 901px)');
    const events = new AbortController();
    let disposed = false, drag = null, frame = 0;
    let values = { ...defaults };
    try {
        const saved = JSON.parse(localStorage.getItem(storageKey));
        if (saved?.version === 1) {
            for (const key of Object.keys(defaults))
                if (typeof saved[key] === 'number' && Number.isFinite(saved[key]))
                    values[key] = clamp(saved[key], key === 'columns' ? .30 : .10, .85);
        }
    } catch { /* Restricted storage must not disable resizing. */ }

    const bounds = key => {
        const size = key === 'columns' ? root.clientWidth - 14 : sidebar.clientHeight - 14;
        const min = key === 'columns' ? Math.max(.30, 240 / Math.max(1, size)) : Math.max(.10, 104 / Math.max(1, size));
        const max = Math.min(.85, 1 - (key === 'columns' ? 240 : 104) / Math.max(1, size));
        return { min: Math.min(min, .5), max: Math.max(max, .5), size };
    };
    const currentShare = key => values[key] ?? sidebar.firstElementChild.getBoundingClientRect().height / Math.max(1, bounds(key).size);
    const apply = () => {
        if (disposed || !desktop.matches) return;
        root.classList.toggle('has-row-split', values.rows !== null);
        for (const handle of handles) {
            const key = handle.dataset.boardSplit;
            const { min, max } = bounds(key);
            const share = clamp(currentShare(key), min, max);
            root.style.setProperty(`--board-${key}-first`, `${share}fr`);
            root.style.setProperty(`--board-${key}-last`, `${1 - share}fr`);
            handle.setAttribute('aria-valuemin', Math.ceil(min * 100));
            handle.setAttribute('aria-valuemax', Math.floor(max * 100));
            handle.setAttribute('aria-valuenow', Math.round(share * 100));
        }
    };
    const persist = () => {
        try { localStorage.setItem(storageKey, JSON.stringify({ version: 1, ...values })); }
        catch { /* Session-only resizing remains available. */ }
    };
    const fit = () => {
        if (disposed) return;
        if (desktop.matches) {
            const top = Math.max(0, root.getBoundingClientRect().top);
            root.style.setProperty('--board-section-top', `${Math.floor(top)}px`);
        }
        apply();
    };
    const finish = (cancel = false) => {
        if (!drag) return;
        const previous = drag; drag = null;
        cancelAnimationFrame(frame); frame = 0;
        if (cancel) values[previous.key] = previous.original;
        root.classList.remove('is-resizing-columns', 'is-resizing-rows');
        if (previous.handle.hasPointerCapture(previous.pointerId)) previous.handle.releasePointerCapture(previous.pointerId);
        apply();
        if (!cancel) persist();
    };
    const listen = (target, name, handler) => target.addEventListener(name, handler, { signal: events.signal });
    for (const handle of handles) {
        const key = handle.dataset.boardSplit;
        listen(handle, 'pointerdown', event => {
            if (!desktop.matches || event.button !== 0 || drag) return;
            event.preventDefault(); handle.focus({ preventScroll: true });
            const { min, max } = bounds(key);
            const initial = clamp(currentShare(key), min, max);
            drag = { key, handle, pointerId: event.pointerId, initial, original: values[key],
                start: key === 'columns' ? event.clientX : event.clientY };
            handle.setPointerCapture(event.pointerId);
            root.classList.add(`is-resizing-${key}`);
        });
        const move = event => {
            if (!drag || drag.pointerId !== event.pointerId || drag.handle !== handle) return;
            const { min, max, size } = bounds(key);
            const delta = (key === 'columns' ? event.clientX : event.clientY) - drag.start;
            values[key] = clamp(drag.initial + delta / Math.max(1, size), min, max);
            if (!frame) frame = requestAnimationFrame(() => { frame = 0; apply(); });
        };
        listen(handle, 'pointermove', move);
        listen(handle, 'pointerup', event => { move(event); finish(); });
        listen(handle, 'pointercancel', () => finish(true));
        listen(handle, 'lostpointercapture', () => finish(true));
        listen(handle, 'dblclick', () => { values[key] = defaults[key]; apply(); persist(); });
        listen(handle, 'keydown', event => {
            if (!desktop.matches) return;
            const previous = key === 'columns' ? 'ArrowLeft' : 'ArrowUp';
            const next = key === 'columns' ? 'ArrowRight' : 'ArrowDown';
            if (![previous, next, 'Home', 'End', 'Enter', 'Escape'].includes(event.key)) return;
            event.preventDefault();
            if (event.key === 'Escape') { finish(true); return; }
            const { min, max } = bounds(key);
            const current = clamp(currentShare(key), min, max);
            const step = event.shiftKey ? .10 : .02;
            values[key] = event.key === 'Enter' ? defaults[key] : event.key === 'Home' ? min
                : event.key === 'End' ? max : clamp(current + (event.key === next ? step : -step), min, max);
            apply(); persist();
        });
    }
    root.classList.add('board-resizable');
    const resize = new ResizeObserver(fit);
    resize.observe(root);
    resize.observe(sidebar.firstElementChild);
    listen(window, 'resize', () => { finish(true); fit(); });
    listen(document, 'fullscreenchange', fit);
    listen(desktop, 'change', () => { finish(true); fit(); });
    const removed = new MutationObserver(() => { if (!root.isConnected) dispose(); });
    removed.observe(document.body, { childList: true, subtree: true });
    function dispose() {
        if (disposed) return;
        finish(true); disposed = true;
        events.abort(); resize.disconnect(); removed.disconnect(); cancelAnimationFrame(frame);
    }
    fit();
    return { dispose };
}
