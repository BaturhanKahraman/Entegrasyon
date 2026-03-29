window.unsavedChangesInterop = {
    _handler: null,

    addBeforeUnloadListener: function () {
        if (this._handler) return;
        this._handler = function (e) {
            e.preventDefault();
            e.returnValue = '';
        };
        window.addEventListener('beforeunload', this._handler);
    },

    removeBeforeUnloadListener: function () {
        if (!this._handler) return;
        window.removeEventListener('beforeunload', this._handler);
        this._handler = null;
    }
};
