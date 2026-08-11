mergeInto(LibraryManager.library, {

    SNEF_EsMovilOTablet: function () {

        var userAgent =
            navigator.userAgent ||
            navigator.vendor ||
            window.opera;

        var esAndroid =
            /Android/i.test(userAgent);

        var esIOS =
            /iPhone|iPad|iPod/i.test(userAgent);

        // Los iPad modernos a veces se identifican
        // como una Mac de escritorio.
        var esIPadModerno =
            navigator.platform === "MacIntel" &&
            navigator.maxTouchPoints > 1;

        var esOtroMovil =
            /Mobile|Tablet|IEMobile|Opera Mini/i.test(userAgent);

        return (
            esAndroid ||
            esIOS ||
            esIPadModerno ||
            esOtroMovil
        ) ? 1 : 0;
    }

});