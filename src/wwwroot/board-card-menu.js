// Native top-layer menu: fullscreen-safe, viewport bounded and keyboard accessible.
export function open(element, x, y, reference) {
    const trigger = document.activeElement;
    let closed = false;
    const items = () => [...element.querySelectorAll('[role="menuitem"]:not(:disabled)')];
    const key = event => {
        const buttons = items();
        const index = buttons.indexOf(document.activeElement);
        let next;
        if (event.key === 'ArrowDown') next = (index + 1) % buttons.length;
        if (event.key === 'ArrowUp') next = (index - 1 + buttons.length) % buttons.length;
        if (event.key === 'Home') next = 0;
        if (event.key === 'End') next = buttons.length - 1;
        if (next !== undefined) { event.preventDefault(); buttons[next]?.focus(); }
        if (event.key === 'Escape' || event.key === 'Tab') {
            event.preventDefault(); element.hidePopover();
        }
    };
    const toggle = event => { if (!closed && event.newState === 'closed') void reference.invokeMethodAsync('DismissAsync'); };
    element.addEventListener('keydown', key);
    element.addEventListener('toggle', toggle);
    element.showPopover();
    const anchor = trigger?.getBoundingClientRect();
    const rect = element.getBoundingClientRect();
    element.style.left = Math.max(8, Math.min(x || anchor?.left || 8, innerWidth - rect.width - 8)) + 'px';
    element.style.top = Math.max(8, Math.min(y || anchor?.bottom || 8, innerHeight - rect.height - 8)) + 'px';
    items()[0]?.focus();
    return { close() {
        closed = true;
        element.removeEventListener('keydown', key);
        element.removeEventListener('toggle', toggle);
        if (element.matches(':popover-open')) element.hidePopover();
        if (trigger?.isConnected) trigger.focus({ preventScroll: true });
    } };
}
