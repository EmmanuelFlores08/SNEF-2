mergeInto(LibraryManager.library, {



    // =========================================================

    // COMPARTIR FOTOGRAFÍA

    // =========================================================



    ShareImageBase64: function (

        base64Ptr,

        filenamePtr,

        titlePtr,

        textPtr

    ) {



        var base64 =

            UTF8ToString(base64Ptr);



        var filename =

            UTF8ToString(filenamePtr);



        var title =

            UTF8ToString(titlePtr);



        var text =

            UTF8ToString(textPtr);





        console.log(

            "[SNEF Share] Botón compartir presionado."

        );





        // =====================================================

        // CONVERTIR BASE64 → FILE PNG

        // =====================================================



        try {



            var bytes =

                atob(base64);





            var byteArray =

                new Uint8Array(

                    bytes.length

                );





            for (

                var i = 0;

                i < bytes.length;

                i++

            ) {



                byteArray[i] =

                    bytes.charCodeAt(i);



            }





            var blob =

                new Blob(

                    [byteArray],

                    {

                        type: "image/png"

                    }

                );





            var file =

                new File(

                    [blob],

                    filename,

                    {

                        type: "image/png"

                    }

                );





            console.log(

                "[SNEF Share] PNG preparado:",

                filename,

                file.size,

                "bytes"

            );





            // =================================================

            // INTENTO 1

            // COMPARTIR CON LA IMAGEN

            // =================================================



            if (

                navigator.share &&

                navigator.canShare

            ) {



                var canShareImage = false;





                try {



                    canShareImage =

                        navigator.canShare({

                            files: [file]

                        });



                }

                catch (error) {



                    console.warn(

                        "[SNEF Share] Error en canShare:",

                        error

                    );



                }





                console.log(

                    "[SNEF Share] ¿Puede compartir PNG?:",

                    canShareImage

                );





                if (canShareImage) {



                    console.log(

                        "[SNEF Share] Abriendo menú CON fotografía."

                    );





                    navigator.share({



                        files: [file],



                        title: title,



                        text: text



                    })



                    .then(function () {



                        console.log(

                            "[SNEF Share] Compartido correctamente."

                        );



                    })



                    .catch(function (error) {



                        console.warn(

                            "[SNEF Share] Compartir cancelado/error:",

                            error

                        );



                    });





                    return;

                }

            }





            // =================================================

            // INTENTO 2

            // MENÚ NATIVO SIN ARCHIVO

            //

            // Este comportamiento estaba en tu proyecto viejo.

            // =================================================



            if (navigator.share) {



                console.log(

                    "[SNEF Share] El PNG no fue aceptado. " +

                    "Abriendo menú nativo sin archivo."

                );





                navigator.share({



                    title: title,



                    text: text



                })



                .then(function () {



                    console.log(

                        "[SNEF Share] Texto compartido correctamente."

                    );



                })



                .catch(function (error) {



                    console.warn(

                        "[SNEF Share] Compartir cancelado/error:",

                        error

                    );



                });





                return;

            }





            // =================================================

            // INTENTO 3

            // FALLBACK

            //

            // Igual que en tu versión vieja:

            // abre X pero NO descarga la fotografía.

            // =================================================



            console.warn(

                "[SNEF Share] Web Share API no disponible. " +

                "Abriendo X como fallback."

            );





            var tweetURL =

                "https://twitter.com/intent/tweet?text=" +

                encodeURIComponent(text);





            window.open(

                tweetURL,

                "_blank"

            );



        }

        catch (error) {



            console.error(

                "[SNEF Share] Error preparando fotografía:",

                error

            );



        }

    },





    // =========================================================

    // X / TWITTER

    // =========================================================



    ShareToTwitter: function (

        textPtr,

        urlPtr

    ) {



        var text =

            UTF8ToString(textPtr);



        var url =

            UTF8ToString(urlPtr);





        var href =

            "https://twitter.com/intent/tweet?text=" +

            encodeURIComponent(text);





        if (url.length > 0) {



            href +=

                "&url=" +

                encodeURIComponent(url);



        }





        window.open(

            href,

            "_blank"

        );

    },





    // =========================================================

    // WHATSAPP

    // =========================================================



    ShareToWhatsApp: function (

        textPtr

    ) {



        var text =

            UTF8ToString(textPtr);





        window.open(

            "https://wa.me/?text=" +

            encodeURIComponent(text),

            "_blank"

        );

    }



});
