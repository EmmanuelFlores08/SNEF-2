mergeInto(LibraryManager.library, {

    // =========================================================
    // DESCARGAR PNG
    // Solo se ejecuta al presionar "Descargar fotografía"
    // =========================================================

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


    // =========================================================
    // COMPARTIR PNG
    // NO descarga la imagen.
    // Intenta compartir exactamente el PNG generado por Unity.
    // =========================================================

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

        console.log(
            "SNEF: intentando compartir fotografía en " +
            network
        );


        // -----------------------------------------------------
        // CONVERTIR LOS BYTES DE UNITY EN UN ARCHIVO PNG REAL
        // -----------------------------------------------------

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


        // -----------------------------------------------------
        // COMPROBAR WEB SHARE
        // -----------------------------------------------------

        if (!navigator.share) {

            console.warn(
                "SNEF: Este navegador no soporta navigator.share()."
            );

            alert(
                "Tu navegador no permite compartir la fotografía directamente. " +
                "Puedes usar el botón Descargar fotografía."
            );

            return;
        }


        // -----------------------------------------------------
        // COMPROBAR SI PUEDE COMPARTIR ARCHIVOS
        // -----------------------------------------------------

        if (navigator.canShare) {

            var canShareFile = false;

            try {

                canShareFile = navigator.canShare({
                    files: [file]
                });

            }
            catch (error) {

                console.warn(
                    "SNEF: Error comprobando canShare:",
                    error
                );
            }


            if (!canShareFile) {

                console.warn(
                    "SNEF: El navegador soporta compartir, " +
                    "pero no archivos PNG."
                );

                alert(
                    "Tu navegador permite compartir contenido, " +
                    "pero no permite adjuntar esta fotografía. " +
                    "Puedes descargarla manualmente."
                );

                return;
            }
        }


        // -----------------------------------------------------
        // COMPARTIR LA FOTO REAL
        // -----------------------------------------------------

        var shareData = {
            files: [file],
            title: title,
            text: text
        };


        navigator.share(shareData)

            .then(function () {

                console.log(
                    "SNEF: fotografía enviada al menú de compartir."
                );

            })

            .catch(function (error) {

                // El usuario simplemente cerró el menú.
                if (error.name === "AbortError") {

                    console.log(
                        "SNEF: el usuario canceló compartir."
                    );

                    return;
                }


                console.error(
                    "SNEF: error compartiendo fotografía:",
                    error
                );


                if (error.name === "NotAllowedError") {

                    console.warn(
                        "SNEF: navigator.share fue bloqueado. " +
                        "Comprueba HTTPS y permisos del iframe."
                    );

                }
            });
    }

});