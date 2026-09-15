mergeInto(LibraryManager.library, {
    SnefMetric: function (tipo, sujeto) {
        try {
            window.SNEF_METRIC(UTF8ToString(tipo), UTF8ToString(sujeto));
        } catch (e) {}
    }
});
