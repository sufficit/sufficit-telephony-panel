// Native top-layer dialog stays above the board, including browser fullscreen.
export function open(dialog, reference) {
    if (!dialog?.isConnected) return { close() {} };
    const trigger = document.querySelector('.board-tile.is-selected') || document.activeElement;
    let closed = false;
    let backdropPointer = false;
    const dismiss = () => {
        if (!closed) void reference.invokeMethodAsync('DismissAsync');
    };
    const cancel = event => { event.preventDefault(); dismiss(); };
    const outside = event => {
        const rect = dialog.getBoundingClientRect();
        return event.target === dialog && (event.clientX < rect.left || event.clientX > rect.right ||
            event.clientY < rect.top || event.clientY > rect.bottom);
    };
    const pointerDown = event => { backdropPointer = outside(event); };
    const click = event => { if (backdropPointer && outside(event)) dismiss(); backdropPointer = false; };
    dialog.addEventListener('cancel', cancel);
    dialog.addEventListener('pointerdown', pointerDown);
    dialog.addEventListener('click', click);
    dialog.showModal();
    return {
        close() {
            if (closed) return;
            closed = true;
            dialog.removeEventListener('cancel', cancel);
            dialog.removeEventListener('pointerdown', pointerDown);
            dialog.removeEventListener('click', click);
            dialog.close();
            if (trigger?.isConnected) trigger.focus({ preventScroll: true });
        }
    };
}
