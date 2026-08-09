mergeInto(LibraryManager.library, {

    SNEF_DownloadPNG: function(dataPtr, dataLength, fileNamePtr) {

        var fileName = UTF8ToString(fileNamePtr);

        var bytes = HEAPU8.slice(
            dataPtr,
            dataPtr + dataLength
        );

        var blob = new Blob(
            [bytes],
            { type: "image/png" }
        );

        var url = URL.createObjectURL(blob);

        var link = document.createElement("a");

        link.href = url;
        link.download = fileName;
        link.style.display = "none";

        document.body.appendChild(link);

        link.click();

        document.body.removeChild(link);

        setTimeout(function () {
            URL.revokeObjectURL(url);
        }, 1000);
    },


    SNEF_SharePNG: function(
        dataPtr,
        dataLength,
        fileNamePtr,
        titlePtr,
        textPtr,
        networkPtr
    ) {

        var fileName = UTF8ToString(fileNamePtr);
        var title = UTF8ToString(titlePtr);
        var text = UTF8ToString(textPtr);
        var network = UTF8ToString(networkPtr);

        var bytes = HEAPU8.slice(
            dataPtr,
            dataPtr + dataLength
        );

        var file = new File(
            [bytes],
            fileName,
            {
                type: "image/png",
                lastModified: Date.now()
            }
        );

        var shareData = {
            title: title,
            text: text,
            files: [file]
        };


        // ---------------------------------------------------
        // WEB SHARE API
        // ---------------------------------------------------

        if (
            navigator.share &&
            navigator.canShare &&
            navigator.canShare({ files: [file] })
        ) {

            navigator.share(shareData)
                .then(function () {

                    console.log(
                        "Fotografía compartida correctamente."
                    );

                })
                .catch(function (error) {

                    /*
                     * AbortError significa normalmente que
                     * el usuario cerró el menú de compartir.
                     */

                    if (error.name !== "AbortError") {

                        console.warn(
                            "No se pudo compartir la fotografía:",
                            error
                        );
                    }

                });

            return;
        }


        // ---------------------------------------------------
        // FALLBACK
        // ---------------------------------------------------

        console.warn(
            "Este navegador no permite compartir archivos directamente. " +
            "Se descargará la fotografía."
        );

        var fallbackBlob = new Blob(
            [bytes],
            { type: "image/png" }
        );

        var fallbackUrl =
            URL.createObjectURL(fallbackBlob);

        var fallbackLink =
            document.createElement("a");

        fallbackLink.href = fallbackUrl;
        fallbackLink.download = fileName;
        fallbackLink.style.display = "none";

        document.body.appendChild(fallbackLink);

        fallbackLink.click();

        document.body.removeChild(fallbackLink);


        setTimeout(function () {

            URL.revokeObjectURL(fallbackUrl);

        }, 1000);


        // Abrimos además la red seleccionada.

        setTimeout(function () {

            switch (network) {

                case "Instagram":

                    window.open(
                        "https://www.instagram.com/",
                        "_blank"
                    );

                    break;


                case "X":

                    window.open(
                        "https://x.com/compose/post?text=" +
                        encodeURIComponent(text),
                        "_blank"
                    );

                    break;


                case "Facebook":

                    window.open(
                        "https://www.facebook.com/",
                        "_blank"
                    );

                    break;


                case "LinkedIn":

                    window.open(
                        "https://www.linkedin.com/feed/",
                        "_blank"
                    );

                    break;
            }

        }, 250);
    }

});