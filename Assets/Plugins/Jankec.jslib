mergeInto(LibraryManager.library, {
    JSShowUI: function() {
        showUI();
    },

    JSPlaceOrigin: function() {
        placeOrigin();
    },

    JSResetOrigin: function() {
        resetOrigin();
    },

    JSIsDesktopPreview: function() {
        return window.__DIGITRON_DESKTOP_PREVIEW ? 1 : 0;
    },
});