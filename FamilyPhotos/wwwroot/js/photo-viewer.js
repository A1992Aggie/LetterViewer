const viewers = new Map();

export function initZoomPan(containerId, dotNetRef) {
    const container = document.getElementById(containerId);
    if (!container) return;

    const state = {
        scale: 1,
        translateX: 0,
        translateY: 0,
        isPanning: false,
        startX: 0,
        startY: 0,
        lastTouchDist: 0,
        dotNetRef
    };

    const img = container.querySelector('img');
    if (!img) return;

    const applyTransform = () => {
        img.style.transform = `translate(${state.translateX}px, ${state.translateY}px) scale(${state.scale})`;
        dotNetRef.invokeMethodAsync('OnZoomChanged', state.scale);
    };

    // Mouse wheel zoom
    container.addEventListener('wheel', (e) => {
        e.preventDefault();
        const delta = e.deltaY > 0 ? 0.9 : 1.1;
        state.scale = Math.max(0.5, Math.min(10, state.scale * delta));
        applyTransform();
    }, { passive: false });

    // Mouse pan
    container.addEventListener('mousedown', (e) => {
        if (e.button !== 0) return;
        state.isPanning = true;
        state.startX = e.clientX - state.translateX;
        state.startY = e.clientY - state.translateY;
        container.style.cursor = 'grabbing';
    });

    document.addEventListener('mousemove', (e) => {
        if (!state.isPanning) return;
        state.translateX = e.clientX - state.startX;
        state.translateY = e.clientY - state.startY;
        applyTransform();
    });

    document.addEventListener('mouseup', () => {
        state.isPanning = false;
        container.style.cursor = 'grab';
    });

    // Touch pinch-to-zoom and pan
    container.addEventListener('touchstart', (e) => {
        if (e.touches.length === 2) {
            e.preventDefault();
            state.lastTouchDist = getTouchDist(e.touches);
        } else if (e.touches.length === 1) {
            state.isPanning = true;
            state.startX = e.touches[0].clientX - state.translateX;
            state.startY = e.touches[0].clientY - state.translateY;
        }
    }, { passive: false });

    container.addEventListener('touchmove', (e) => {
        if (e.touches.length === 2) {
            e.preventDefault();
            const dist = getTouchDist(e.touches);
            const delta = dist / state.lastTouchDist;
            state.scale = Math.max(0.5, Math.min(10, state.scale * delta));
            state.lastTouchDist = dist;
            applyTransform();
        } else if (e.touches.length === 1 && state.isPanning) {
            state.translateX = e.touches[0].clientX - state.startX;
            state.translateY = e.touches[0].clientY - state.startY;
            applyTransform();
        }
    }, { passive: false });

    container.addEventListener('touchend', () => {
        state.isPanning = false;
        state.lastTouchDist = 0;
    });

    container.style.cursor = 'grab';
    viewers.set(containerId, state);
}

export function resetZoom(containerId) {
    const state = viewers.get(containerId);
    if (!state) return;
    state.scale = 1;
    state.translateX = 0;
    state.translateY = 0;
    const container = document.getElementById(containerId);
    const img = container?.querySelector('img');
    if (img) {
        img.style.transform = 'translate(0px, 0px) scale(1)';
        state.dotNetRef.invokeMethodAsync('OnZoomChanged', 1);
    }
}

export function getImageCoordinates(containerId, clientX, clientY) {
    const container = document.getElementById(containerId);
    const img = container?.querySelector('img');
    if (!img) return null;

    const rect = img.getBoundingClientRect();
    const state = viewers.get(containerId);
    if (!state) return null;

    // Account for transform
    const naturalX = (clientX - rect.left) / rect.width * 100;
    const naturalY = (clientY - rect.top) / rect.height * 100;

    return { xPercent: naturalX, yPercent: naturalY };
}

export function dispose(containerId) {
    viewers.delete(containerId);
}

function getTouchDist(touches) {
    const dx = touches[0].clientX - touches[1].clientX;
    const dy = touches[0].clientY - touches[1].clientY;
    return Math.sqrt(dx * dx + dy * dy);
}
