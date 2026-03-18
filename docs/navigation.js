const placeButton = document.getElementById("place-button");
const resetButton = document.getElementById("reset-button");

function sendToUnity(target, method) {
    if (!window.unityInstance) return;
    try {
        window.unityInstance.SendMessage(target, method);
    } catch (e) {
        console.warn("SendMessage failed:", target, method, e);
    }
}

function showUI() {
    if (placeButton) placeButton._show(true);
    if (resetButton) resetButton._show(false);
    // Force clean unplaced state on first load so placement indicator appears.
    sendToUnity("WorldTracker", "ResetOrigin");
    sendToUnity("MainController", "OnResetOrigin");
}

function resetOrigin() {
    if (placeButton) placeButton._show(true);
    if (resetButton) resetButton._show(false);
    sendToUnity("WorldTracker", "ResetOrigin");
    sendToUnity("MainController", "OnResetOrigin");
}

function placeOrigin() {
    if (placeButton) placeButton._show(false);
    if (resetButton) resetButton._show(true);
    sendToUnity("WorldTracker", "PlaceOrigin");
    // Fallback: ensure calculator spawn even if tracker event binding was lost.
    sendToUnity("MainController", "OnPlacedOrigin");
}

HTMLElement.prototype._show = function (toShow) {
    this.classList.toggle("d-none", !toShow);
    return this;
};
