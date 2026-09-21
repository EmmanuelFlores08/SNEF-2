mergeInto(LibraryManager.library, {
    SnefMetric: function (tipo, sujeto) {
        try {
            window.SNEF_METRIC(UTF8ToString(tipo), UTF8ToString(sujeto));
        } catch (e) {}
    },

    SnefComprar: function (itemId, txId) {
        try {
            window.SNEF_COMPRAR(
                UTF8ToString(itemId),
                UTF8ToString(txId)
            );
        } catch (e) {}
    },

    SnefAvatar: function (avatarId) {
        try {
            window.SNEF_AVATAR(
                UTF8ToString(avatarId)
            );
        } catch (e) {}
    }
});
