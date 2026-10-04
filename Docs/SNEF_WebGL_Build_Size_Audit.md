# SNEF 2026 - WebGL Build Size Audit

Fecha de auditoría: 3 de octubre de 2026  
Proyecto: Unity 6000.0.46f1, URP 17.0.4  
Alcance: inspección y medición. No se eliminó, recomprimió ni reimportó ningún asset; no se modificaron escenas ni ajustes.

## Resumen ejecutivo

El build WebGL más reciente y exitoso termina en **188.48 MiB sin compresión HTTP/precompresión**. El mismo contenido, medido en el build hermano `BROT`, ocupa **145.78 MiB transferibles con Brotli**. Por tanto, aproximadamente **42.70 MiB** del problema actual no es contenido sino la forma de publicación/compresión del build.

El cuello de botella real después de Brotli son las texturas. Unity reporta **327.0 MiB de memoria/tamaño descomprimido de texturas**, equivalentes al **78.5%** de los assets de usuario. El dato no debe sumarse directamente al archivo `.data`: Unity informa el tamaño descomprimido o de representación importada, mientras que `.data` guarda una representación empaquetada. Aun así, sirve para ordenar el impacto relativo y revela una familia grande de sprites UI/posters que conserva resoluciones muy superiores a su tamaño de pantalla.

Objetivo realista: **120 MiB de transferencia** después de asegurar Brotli y ejecutar una pasada visual controlada sobre sprites/posters y los dos audios largos. **100 MiB** es un objetivo de fase estructural y necesita builds A/B; **80 MiB no está respaldado por los datos actuales** sin externalizar o rediseñar una cantidad importante de contenido.

## Tamaño actual del build

Build auditado: `C:/Mis archivos/Builds Unity/BuildsV1/Build_SNEF2026_031026`  
Fecha del build: 3 de octubre de 2026, 21:51  
Resultado: `Succeeded` en 47.4 s  
BuildReport: `Library/LastBuild.buildreport` y `Editor.log`

| Archivo | Sin comprimir | Brotli medido | Transferencia esperada |
|---|---:|---:|---:|
| `.data` | 162.73 MiB | 139.90 MiB | 139.90 MiB |
| `.wasm` | 25.30 MiB | 5.77 MiB | 5.77 MiB |
| `.framework.js` | 0.42 MiB | 0.07 MiB | 0.07 MiB |
| `.loader.js` | 25.8 KiB | 25.8 KiB | 25.8 KiB |
| `index.html` | 11.7 KiB | sin precompresión | 11.7 KiB |
| **Total** | **188.48 MiB** | **145.78 MiB** | **~145.8 MiB** |

No hay `.gz` en el build reciente. El build `BROT` contiene `.data.br`, `.wasm.br` y `.framework.js.br`. La descompresión local de esos archivos dio exactamente 162.73 MiB para data, 25.30 MiB para wasm y 0.42 MiB para framework, por lo que es una comparación directa y no una proyección teórica.

La cifra de transferencia asume que el servidor entrega los `.br` con `Content-Encoding: br` y MIME correctos. Si se sirve el build sin esos headers o se publica la carpeta sin compresión, el navegador descargará aproximadamente 188.5 MiB.

Builds encontrados fuera del proyecto:

| Build | Total en disco | Estado |
|---|---:|---|
| `Build_SNEF2026_031026` | 188.48 MiB | reciente, sin archivos `.br/.gz` |
| `BROT` | 145.78 MiB | Brotli, mismo tamaño descomprimido que el build reciente |
| `Build_SNEF2026_240926` | 183.53 MiB | sin precompresión |
| `Build_SNEF2026_210926` | 360.60 MiB | contiene dos juegos de artefactos; no usar como referencia |
| `Buiold_09_Brotli` | 139.21 MiB | Brotli, contenido anterior más pequeño |

No se encontraron builds WebGL finales dentro de la carpeta del proyecto; sí quedaron artefactos intermedios en `Library/Bee/artifacts/WebGL`.

## Distribución por tipo de archivo

Datos del BuildReport; son tamaños descomprimidos/de representación importada y sirven para atribución relativa, no equivalen uno a uno a bytes de red.

| Categoría Unity | Tamaño reportado | Porcentaje |
|---|---:|---:|
| Texturas | 327.0 MiB | 78.5% |
| Otros assets | 62.9 MiB | 15.1% |
| Meshes | 14.7 MiB | 3.5% |
| Audio | 7.6 MiB | 1.8% |
| Shaders | 3.7 MiB | 0.9% |
| Animaciones | 407.5 KiB | 0.1% |
| Headers | 196.1 KiB | <0.1% |
| **Assets de usuario** | **416.7 MiB** | **100%** |

Desglose adicional de las 1,983 entradas por extensión (357.25 MiB con ruta individual; el resto corresponde a datos internos/engine y redondeo):

| Tipo | Entradas | Tamaño reportado |
|---|---:|---:|
| PNG | 304 | 186.05 MiB |
| JPG | 120 | 138.08 MiB |
| FBX/modelos | 246 | 14.78 MiB |
| MP3 + WAV | 15 | 7.58 MiB |
| `.asset` / ScriptableObjects y datos | 29 | 3.45 MiB |
| Shaders + Shader Graph | 51 | 3.77 MiB |
| Fonts TTF | 7 | 0.78 MiB |
| EXR/light/reflection | 2 | 0.46 MiB |
| Prefabs | 43 | 0.44 MiB |
| Animaciones + controllers | 20 | 0.41 MiB |
| PSD + TGA | 5 | 0.61 MiB |
| Materials | 88 | 0.16 MiB |
| Scripts C# listados | 1,037 | 0.12 MiB |
| RenderTextures | 7 | <0.01 MiB serializados; su memoria se asigna en runtime |
| VideoClip MOV | 1 | 0.2 KiB de objeto; video fuente no contabilizado |
| Plugins WebGL | 4 `.jslib` | 9.7 KiB fuente; contribución despreciable |

El proyecto fuente ocupa 612.5 MiB en `Assets`; esa cifra no es comparable directamente con el build porque incluye demos, fuentes originales y escenas YAML que Unity transforma. `Library` ocupa 4.17 GiB y tampoco forma parte de la entrega web.

## Top 50 assets que más pesan

`Tamaño` es el valor descomprimido del BuildReport. `Fuente/configuración` ayuda a explicar por qué un PNG/JPG pequeño en Git puede transformarse en varios MiB de memoria importada. Todos los elementos de esta tabla **sí entran al build**.

| # | Asset | Tipo | Tamaño | Fuente/configuración relevante | Motivo de inclusión |
|---:|---|---|---:|---|---|
| 1 | `Assets/Sprites/Fondos/Nu_Claro.png` | Texture/Sprite | 14.1 MiB | 1925×1925; 0.03 MiB fuente; max 2048; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 2 | `Assets/Sprites/Posters/Sala de Cines/BancoAzteca/BancoAzteca_Claro.png` | Texture/Sprite | 12.4 MiB | 3300×2550; max 2048; sin mipmaps/read-write | `Cine.unity` |
| 3 | `Assets/Sprites/Posters/Sala de Cines/BancoAzteca/BancoAzteca_Oscuro.png` | Texture/Sprite | 12.4 MiB | 3300×2550; max 2048; sin mipmaps/read-write | `Cine.unity` y `PhotoKitCatalog.asset` |
| 4 | `Assets/Scenes/PruebasJoshi/UI/arco.png` | Texture/Sprite | 6.7 MiB | 1291×1355; 1.14 MiB fuente; sin mipmaps/read-write | `Cine.unity` |
| 5 | `Assets/Sprites/UI/Paneles/panelBG.png` | Texture/Sprite | 5.6 MiB | 1512×966; max 2048; sin mipmaps/read-write | ambas escenas del build |
| 6 | `Assets/Models/Materials/Decoraciones/MrEngaños_upscayl_4x_upscayl-standard-4x 1.png` | Texture | 5.3 MiB | 4096×4096; importado max 2048; mipmaps; 8.89 MiB fuente | material `MrEngañosCarton.mat` → escena/prefab |
| 7 | `Assets/Scenes/PruebasJoshi/UI/SelectorPeliculas/contenedorPeliculas.png` | Texture/Sprite | 4.4 MiB | 1624×707; sin mipmaps/read-write | `Cine.unity` |
| 8 | `Assets/Sprites/Posters/Sala de Cines/Clip/Clip_Claro.png` | Texture/Sprite | 4.0 MiB | 2280×2280; max 2048; sin mipmaps/read-write | `Cine.unity` |
| 9 | `Assets/Sprites/UI/TactilControls/UI_Circle_Faded.png` | Texture/Sprite | 4.0 MiB | 2048×2048; sin mipmaps/read-write | `Cine.unity` |
| 10 | `Assets/Sounds/Music/Fotos.mp3` | Audio | 3.8 MiB | 1.59 MiB fuente; Vorbis quality 1; 44.1 kHz; stereo; Decompress On Load | `Cine.unity` |
| 11 | `Assets/Sounds/Music/Fondo.mp3` | Audio | 3.6 MiB | 1.49 MiB fuente; Vorbis quality 1; 44.1 kHz; stereo; Decompress On Load | `Cine.unity` |
| 12 | `Assets/Scenes/PruebasJoshi/UI/SelectorPeliculas/Quiz/PanelResultadoQuiz.png` | Texture/Sprite | 3.4 MiB | 1112×790; sin mipmaps/read-write | `Cine.unity` |
| 13 | `Assets/Sprites/UI/Paneles/logo-oscuro 2.png` | Texture/Sprite | 3.1 MiB | 1041×781; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 14 | `Assets/Scenes/PruebasJoshi/UI/SelectorPeliculas/Quiz/contenedorBotones.png` | Texture/Sprite | 3.0 MiB | 1576×498; sin mipmaps/read-write | `Cine.unity` |
| 15 | `Assets/Scenes/PruebasJoshi/UI/Taquilla/containerElementosTaquilla.png` | Texture/Sprite | 2.9 MiB | 1233×625; sin mipmaps/read-write | `Cine.unity` |
| 16 | `Assets/Sprites/Posters/Sala de Cines/STP/logo-oscuro.png` | Texture/Sprite | 2.9 MiB | 1920×389; sin mipmaps/read-write | `Cine.unity` |
| 17 | `Assets/Sprites/Posters/Sala de Cines/STP/logo-claro.png` | Texture/Sprite | 2.9 MiB | 1920×389; sin mipmaps/read-write | `Cine.unity` y `PhotoKitCatalog.asset` |
| 18 | `Assets/Models/Materials/Decoraciones/dusef.png` | Texture | 2.7 MiB | 2505×3426; max 2048; mipmaps; 4.18 MiB fuente | material `DusefCarton.mat` → escena/prefab |
| 19 | `Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader` | Shader | 2.5 MiB | URP/Lit; variantes reducidas por stripping | materiales URP usados |
| 20 | `Assets/Scenes/PruebasJoshi/UI/SetDeGrabación/containerUISet.png` | Texture/Sprite | 2.4 MiB | 973×649; sin mipmaps/read-write | `Cine.unity` |
| 21 | `Assets/Sprites/Fondos/Banxico_LogoOscuro.png` | Texture/Sprite | 2.4 MiB | 2250×331; max 2048; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 22 | `Assets/Sprites/Posters/Oro/Banxico/Poster_3.png` | Texture/Sprite | 2.0 MiB | 600×870; sin mipmaps/read-write | `Cine.unity` |
| 23 | `Assets/Scenes/PruebasJoshi/UI/Instrucciones/slide5-setDeGrabacion.png` | Texture/Sprite | 2.0 MiB | 1924×1084; sin mipmaps/read-write | `Cine.unity` |
| 24 | `Assets/Scenes/PruebasJoshi/UI/Instrucciones/slide2-controlesCelular.png` | Texture/Sprite | 2.0 MiB | 1924×1084; sin mipmaps/read-write | `Cine.unity` |
| 25 | `Assets/Scenes/PruebasJoshi/UI/Instrucciones/slide3-salasDeCine.png` | Texture/Sprite | 2.0 MiB | 1924×1084; sin mipmaps/read-write | `Cine.unity` |
| 26 | `Assets/Scenes/PruebasJoshi/UI/Instrucciones/slide2-controlesPC.png` | Texture/Sprite | 2.0 MiB | 1924×1084; sin mipmaps/read-write | `Cine.unity` |
| 27 | `Assets/Scenes/PruebasJoshi/UI/Instrucciones/slide1-bienvenida.png` | Texture/Sprite | 2.0 MiB | 1924×1084; sin mipmaps/read-write | `Cine.unity` |
| 28 | `Assets/Scenes/PruebasJoshi/UI/Instrucciones/slide4-taquilla.png` | Texture/Sprite | 2.0 MiB | 1924×1084; sin mipmaps/read-write | `Cine.unity` |
| 29 | `Assets/Sprites/UI/Paneles/Condusef_Taquilla.jpg` | Texture/Sprite | 1.9 MiB | 810×810; 0.08 MiB fuente; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 30 | `Assets/Sprites/UI/Paneles/Bienesta_Taquilla.jpg` | Texture/Sprite | 1.9 MiB | 810×810; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 31 | `Assets/Sprites/UI/Paneles/Bankaool_Taquilla.jpg` | Texture/Sprite | 1.9 MiB | 810×810; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 32 | `Assets/Sprites/UI/Paneles/BancoAzteca_Taquilla.jpg` | Texture/Sprite | 1.9 MiB | 810×810; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 33 | `Assets/Sprites/UI/Paneles/AforeCoppel_Taquilla.jpg` | Texture/Sprite | 1.9 MiB | 810×810; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 34 | `Assets/Sprites/UI/Paneles/Finsus_Taquilla.jpg` | Texture/Sprite | 1.9 MiB | 810×810; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 35 | `Assets/Sprites/UI/Paneles/Banxico_Taquilla.jpg` | Texture/Sprite | 1.9 MiB | 810×810; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 36 | `Assets/Sprites/UI/Paneles/AMAFORE_Taquilla.jpg` | Texture/Sprite | 1.9 MiB | 810×810; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 37 | `Assets/Sprites/UI/Paneles/STP_Taquilla.jpg` | Texture/Sprite | 1.9 MiB | 810×810; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 38 | `Assets/Sprites/UI/Paneles/Nu_Taquilla.jpg` | Texture/Sprite | 1.9 MiB | 810×810; sin mipmaps/read-write | `PhotoKitCatalog.asset` |
| 39 | `Assets/Scenes/PruebasJoshi/UI/SelectorPeliculas/Quiz/RespuestaIncorrectaPanel.png` | Texture/Sprite | 1.9 MiB | 958×509; sin mipmaps/read-write | `Cine.unity` |
| 40 | `Assets/Scenes/PruebasJoshi/UI/Taquilla/ADVERTENCIA.png` | Texture/Sprite | 1.9 MiB | 958×509; sin mipmaps/read-write | `Cine.unity` |
| 41 | `Assets/Sprites/Pantalllas/Dusef cinepolito.png` | Texture/Sprite | 1.9 MiB | 559×870; 0.35 MiB fuente; sin mipmaps/read-write | `Cine.unity` |
| 42 | `Assets/Sprites/UI/Paneles/cardAvatars.png` | Texture/Sprite | 1.8 MiB | 751×611; sin mipmaps/read-write | `SelectorAvatar.unity` |
| 43 | `Assets/Scenes/PruebasJoshi/UI/SelectorPeliculas/Quiz/contenedorPelicula.png` | Texture/Sprite | 1.7 MiB | 1576×282; sin mipmaps/read-write | `Cine.unity` |
| 44 | `Assets/Sprites/Posters/Sala de Cines/STP/Poster_5.jpg` | Texture/Sprite | 1.5 MiB | 600×870; sin mipmaps/read-write | `Cine.unity` |
| 45 | `Assets/Sprites/Posters/Sala de Cines/STP/Poster_4.jpg` | Texture/Sprite | 1.5 MiB | 600×870; sin mipmaps/read-write | `Cine.unity` |
| 46 | `Assets/Sprites/Posters/Sala de Cines/STP/Poster_3.jpg` | Texture/Sprite | 1.5 MiB | 600×870; sin mipmaps/read-write | `Cine.unity` |
| 47 | `Assets/Sprites/Posters/Sala de Cines/STP/Poster_2.jpg` | Texture/Sprite | 1.5 MiB | 600×870; sin mipmaps/read-write | `Cine.unity` |
| 48 | `Assets/Sprites/Posters/Sala de Cines/STP/Poster_1.jpg` | Texture/Sprite | 1.5 MiB | 600×870; sin mipmaps/read-write | `Cine.unity` |
| 49 | `Assets/Sprites/Posters/Sala de Cines/Clip/Posters/Poster_5.jpg` | Texture/Sprite | 1.5 MiB | 600×870; sin mipmaps/read-write | `Cine.unity` |
| 50 | `Assets/Sprites/Posters/Sala de Cines/Clip/Posters/Poster_4.jpg` | Texture/Sprite | 1.5 MiB | 600×870; sin mipmaps/read-write | `Cine.unity` |

## Texturas

- El BuildReport contiene 431 entradas de imagen: 304 PNG, 120 JPG, 3 TGA, 2 PSD y 2 EXR. Las texturas del proyecto `Assets` auditables suman 322.4 MiB descomprimidos en el reporte.
- Entre las 379 texturas fuente de `Assets` que entran: 1 tiene lado de 4096, 7 superan 2048 en origen y 65 superan 1024.
- 372 usan `Max Size 2048`, 4 usan 1024, 2 usan 512 y 1 usa 64.
- Ninguna de las 379 tiene Read/Write activo. Esto ya está bien.
- 46 tienen mipmaps; la mayoría de UI/sprites no los tiene. Esto también está bien.
- Los importadores WebGL observados usan el formato automático y compresión normal; no hay un override WebGL específico en los mayores offenders.
- Los PNG/JPG de colores planos son muy pequeños como archivo fuente pero se expanden al importarse. `Nu_Claro.png` pasa de 0.03 MiB fuente a 14.1 MiB reportados; los JPG de 600×870 pasan de 0.02–0.09 MiB a ~1.5 MiB cada uno.
- `alphaUsage: 1` aparece incluso en JPG, por lo que ese campo serializado no demuestra que exista alpha útil. Antes de forzar formatos sin alpha se debe ejecutar la herramienta de Editor, que usa `DoesSourceTextureHaveAlpha()`, y hacer un build A/B.
- No se detectaron texturas de 8K utilizadas. La única 4K utilizada ya se limita a 2048, aunque conviene confirmar si necesita 2048 y mipmaps.

Oportunidades principales:

1. Reducir a 1024 las imágenes UI que nunca ocupan más de 1024 píxeles en pantalla, empezando por `Nu_Claro`, `BancoAzteca_*`, `Clip_Claro`, `UI_Circle_Faded` y los seis slides de instrucciones.
2. Revisar el catálogo masivo de posters 600×870 y tarjetas 810×810. Cada uno parece pequeño en el repositorio, pero Unity reporta 1.5–1.9 MiB descomprimidos por imagen; la suma del grupo es grande.
3. Confirmar formato WebGL final (ASTC/ETC2/DXT según subtarget) con el CSV del próximo build. No asignar RGBA32/RGB24 manualmente sin esa comprobación.
4. Evaluar SpriteAtlas por familia para reducir overhead/draw calls. No asumir ahorro de transferencia: el ahorro principal vendrá de resolución/formato.

## Modelos 3D

- Existen 4,513 FBX/OBJ fuente por 302.8 MiB, pero solo 246 aparecen en el build y suman 14.78 MiB reportados. La mayor parte de los packs 3D no afecta el build actual.
- Los 246 modelos usados tienen `Mesh Compression: Off` y `Read/Write: Off`.
- 177 importan animación y BlendShapes; 125 son Humanoid, 52 Generic y 69 no importan animación. En un sistema de avatares personalizables, no es seguro desactivar rig/BlendShapes globalmente.
- `Optimize Game Objects` está desactivado en los 246. Puede ahorrar jerarquía/CPU en personajes, pero puede romper referencias a huesos y requiere pruebas.
- Mayores meshes del build: `VendingMachine_01.fbx` 602.8 KiB, `Base_Model.fbx` 427.7 KiB, `JuiceDispenserBlenderFile.fbx` 393.8 KiB, tres zapatos de 313–324 KiB y `Studio equipment.fbx` 312.3 KiB.
- No se pudo obtener el conteo fiable de vértices/polígonos sin cargar cada modelo mediante Unity. La herramienta creada registra import settings y tamaño empaquetado; una segunda versión puede añadir `Mesh.vertexCount` si se desea pagar el costo de carga.

Conclusión: mesh compression puede dar **2–5 MiB** de ahorro, pero no es la primera palanca y debe probarse visualmente en los modelos mayores.

## Audio

| Clip | BuildReport | Fuente | Configuración |
|---|---:|---:|---|
| `Fotos.mp3` | 3.8 MiB | 1.59 MiB | Vorbis quality 1, 44.1 kHz, stereo, Decompress On Load |
| `Fondo.mp3` | 3.6 MiB | 1.49 MiB | Vorbis quality 1, 44.1 kHz, stereo, Decompress On Load |
| 7 clips UI MP3 | ~0.16 MiB | ~0.07 MiB | misma calidad máxima |
| 6 pasos WAV | ~0.06 MiB | ~0.24 MiB | misma calidad máxima |

`Decompress On Load` afecta principalmente memoria en ejecución, no el peso transferido si el clip sigue comprimido en el build. Para los dos temas largos conviene probar `Streaming` o `Compressed In Memory` por memoria/arranque y reducir Vorbis quality de 1.0 a 0.55–0.7. Ahorro de transferencia estimado: **1–2 MiB**, sujeto a build A/B y escucha. Forzar mono solo es seguro si la imagen estéreo no es importante.

## Video

| Video fuente | Fuente | Estado en último build |
|---|---:|---|
| `Assets/Sprites/Videos/ORIGINAL_DUSEF.mov` | 59.93 MiB | aparece como objeto de solo 0.2 KiB; el binario de 59.93 MiB no se contabiliza como asset empaquetado |
| `Assets/Scenes/PruebasJoshi/Recursos de prueba/CondusefVideo03.mp4` | 16.70 MiB | fuera del build; solo referenciado por la escena deshabilitada `Cine Joshi.unity` |

No hay video dentro de `Resources` o `StreamingAssets`. El código de sala ya admite `VideoSource.Url`, lo que es apropiado para CDN. Con los datos actuales no corresponde asignar ahorro al externalizar video, porque el video pesado no está aumentando el build auditado. Si en un build futuro el `.mov/.mp4` aparece con tamaño material en el CSV, marcarlo como **alta prioridad**.

## Lightmaps

| Asset | Tamaño reportado |
|---|---:|
| `Assets/Scenes/Cine/LightingData.asset` | 911.5 KiB |
| `Lightmap-0_comp_shadowmask.png` | 682.8 KiB |
| `Lightmap-0_comp_light.exr` | 341.5 KiB |
| `ReflectionProbe-0.exr` | 128.4 KiB |
| **Total principal** | **~2.02 MiB** |

Hay un solo lightmap principal, un shadowmask y un Reflection Probe relevantes. No constituyen el problema central. Reducirlos antes que las texturas UI daría poco retorno y mayor riesgo visual.

## Shaders

- 14 `.shader`, 8 `.shadergraph` y 1 `.shadersubgraph` dentro de `Assets`.
- El BuildReport atribuye 3.7 MiB a shaders; URP/Lit por sí solo figura con 2.5 MiB descomprimidos.
- El log registró 230 compilaciones de pass/stage. El espacio teórico sumó 14,726,738,257 variantes, dominado por URP/Lit (13,589,544,960) y TMP SDF URP Lit (1,132,462,080).
- Filtrado por settings: 166,563; después de stripping built-in: 4,444; después de stripping scriptable: **1,664**.
- No hay Shader Variant Collections precargadas. `GraphicsSettings` conserva todas las variantes de lightmap y niebla, pero el stripping final ya es agresivo.

Existe una explosión teórica, pero el resultado final es pequeño frente a texturas. Prioridad media/baja: crear un build de referencia y luego probar stripping de modos de niebla/lightmap realmente no usados. No tocar Always Included Shaders sin pruebas funcionales.

## Resources

- No existe actualmente `Assets/Resources` propio.
- Sí existen 39 archivos dentro de carpetas `Resources` de TextMesh Pro, con 4.34 MiB fuente.
- El BuildReport atribuye 2.077 MiB a 44 entradas `Resources` (incluye recursos virtuales de paquetes).
- Los dos mayores son `LiberationSans SDF.asset` (~1 MiB) y `TextMesh Pro/Examples & Extras/.../Unity SDF.asset` (~1 MiB).
- No hay llamadas `Resources.Load` en código propio. Las únicas llamadas encontradas están en ejemplos de TMP.

La carpeta `TextMesh Pro/Examples & Extras/Resources` entra por la regla de Resources aunque sus escenas no se usen. Migrar o retirar Examples & Extras del proyecto sería un cambio posterior de bajo riesgo si se verifica que ninguna UI usa esos font assets/materiales.

## StreamingAssets

No existe `Assets/StreamingAssets` y no se detectó contribución de StreamingAssets al build.

## Packages

Contribución descomprimida registrada por el BuildReport:

| Paquete | Entradas | Tamaño reportado |
|---|---:|---:|
| URP | 299 | 6.03 MiB |
| Render Pipelines Core | 300 | 0.31 MiB |
| UGUI, Mathematics, Post Processing, Timeline, Burst, Collections | 449 combinadas | ~0.06 MiB |
| Multiplayer Center / Recorder | 6 | redondea a 0.00 MiB |

`com.unity.recorder` y `com.unity.multiplayer.center` son candidatos a revisar porque normalmente son herramientas de Editor, pero el ahorro de build medido es despreciable. `com.unity.postprocessing` 3.5.4 parece legado en un proyecto URP y define `UNITY_POST_PROCESSING_STACK_V2`, aunque su contribución reportada es ~0.01 MiB. No desinstalar hasta confirmar que no hay Volumes/componentes antiguos.

Plugins de `Assets/Plugins/WebGL`: cuatro `.jslib`, 9.7 KiB fuente en total (`SharePlugin`, `Download`, `SNEFMobileDetector`, `SnefMetrics`). No son un objetivo de tamaño.

## Assets potencialmente no utilizados

El último BuildReport contiene 1,983 entradas; 9,094 archivos fuente bajo `Assets` no aparecen como payload individual. Esto no significa que deban borrarse. La clasificación se basa en el build real, referencias YAML y ausencia de Addressables/AssetBundles/Resources.Load propio.

### SEGURO PROBABLEMENTE NO UTILIZADO por el build de producción

Estos elementos no entran al último build, no están en Resources/StreamingAssets y pertenecen principalmente a demos o material auxiliar:

- Escenas demo de `LowPolyTropicalCity`, `LowPolyInterior`, `LowPolyDungeons`, `LowPolyFantasyArena`, `LowPolyOfficeInterior` y las tres escenas Overview de Cute Characters.
- 19 copias idénticas de `README_Bundle_1.25.pdf`.
- `Assets/Sounds/UI/freesound_community-userinterface-32114.mp3` (1.19 MiB), sin referencia encontrada.
- Animaciones `Skeleton_01_Death_Backward.anim`, `Skeleton_01_Fall.anim`, `Skeleton_01_Dodge_Roll.anim`, sin referencia YAML encontrada.
- `Assets/Scenes/PruebasJoshi/UI/image 662.png`, sin referencia encontrada.

### REQUIERE REVISIÓN

- `CondusefVideo03.mp4` (16.70 MiB): excluido del build de producción, pero usado por `Cine Joshi.unity` deshabilitada.
- `Animation.fbx` (6.73 MiB): excluido directamente, pero referenciado por `Animation.prefab` fuera del build.
- Prefabs grandes de los paquetes de VFX: varios están referenciados por sus propias escenas demo; algunos también aparecen en `Cine.unity`, pero el BuildReport no los lista como payload individual. Revisar objetos deshabilitados, referencias EditorOnly y sub-assets antes de cualquier limpieza.
- `PantallaDeCarga.jpg` duplicado en dos rutas; confirmar cuál usa el template/escena.
- Assets de escenas `Cine Joshi`, `Cine2`, `SetDeGrabacion` y `Pruebas`: no afectan el build actual, pero pueden ser fuentes de trabajo o respaldo.

### NO ELIMINAR

- Las dos escenas habilitadas y cualquiera de sus dependencias.
- `PhotoKitCatalog.asset` y todo asset referenciado por él; explica gran parte del top de texturas.
- Contenido de TMP Resources necesario para el fallback/default font.
- Scripts y `.jslib`, aunque su tamaño individual aparezca como 0.0 KiB redondeado.
- Cualquier asset cargado mediante URL/string en contenido futuro. No hay Addressables, labels de AssetBundle ni carga dinámica propia detectada hoy, pero esto puede cambiar.

Los archivos no utilizados no inflan el build actual; limpiar el repositorio podría mejorar mantenimiento/importación, pero produce **0 MiB de ahorro de red** mientras sigan excluidos.

## Duplicados

Hash SHA-256 de 10,017 archivos no-meta:

- 11 grupos binariamente idénticos.
- 39 archivos participan en esos grupos.
- Redundancia fuente aproximada: 3.83 MiB, casi toda fuera del build.
- Tres escenas `Cute_Characters/.../Overview.unity` son idénticas: 2.07 MiB redundantes, pero están fuera del build.
- `PantallaDeCarga.jpg` está duplicado entre `Assets/Scenes/PruebasJoshi/UI` y `Assets/UI`: 0.88 MiB redundante; ambos fuera del último payload según el reporte.
- 19 PDFs README idénticos: 0.84 MiB redundante; no entran al build.
- `Condusef_Mat.png` y `CondusefLogo_Mat.png` son idénticos (30.7 KiB cada uno).
- También hay cinco grupos pequeños de iconos UI PNG idénticos.

No se realizó comparación perceptual de imágenes similares; solo hashes exactos. Consolidar duplicados no es prioridad de tamaño WebGL salvo que dos copias entren simultáneamente en un build futuro.

## Configuración WebGL actual

El proyecto tiene cambios locales previos a la auditoría en `ProjectSettings.asset` y en el build profile. Por eso se documentan ambos y se evita atribuir el último build a una configuración que pudo cambiar después.

| Ajuste | Global `ProjectSettings` | Build Profile `SNEF2.0` | Observación |
|---|---|---|---|
| Compression Format | valor serializado 2 (Brotli) | hereda (`m_CompressionType: -1`) | el último output quedó sin `.br`; existe build hermano Brotli válido |
| Decompression Fallback | Off | On | discrepancia; fallback puede aumentar output/CPU |
| Data Caching | On | On | adecuado para revisitas |
| Managed Stripping | nivel 3 (Medium) | nivel 3 | ya activo |
| Strip Engine Code | On | On | ya activo |
| Strip Unused Mesh Components | On | Off | discrepancia; el perfil conserva componentes |
| Exception Support | valor 1 (Explicitly Thrown) | igual | compromiso razonable |
| Debug Symbols | Off | Off | correcto para release |
| Development Build | — | Off | correcto |
| Script Debugging | — | Off | correcto |
| Deep Profiling / Profiler | — | Off | correcto |
| IL2CPP config | Release | perfil hereda | correcto |
| IL2CPP code generation | Optimize Size | Optimize Size | correcto |
| WebGL code optimization | — | valor 2, Disk Size | orientado a tamaño |
| Texture subtarget | default | 0 | verificar el formato real en navegador objetivo |
| WebGL Analyze Build Size | Off | Off | la herramienta añadida cubre el diagnóstico |
| URP | activo | activo | Quality WebGL usa perfil `Mobile` |

El artefacto final es la fuente de verdad: hoy el build más reciente no está precomprimido aunque el YAML actual indique Brotli. Antes de publicar, verificar el build generado y los headers del hosting.

## Escenas

Escenas habilitadas:

| Escena | Level comprimido | Level descomprimido | Participación |
|---|---:|---:|---|
| `Assets/Scenes/SelectorAvatar.unity` | 12.9 MiB | 48.0 MiB | selector/personajes/UI |
| `Assets/Scenes/Cine.unity` | 76.0 MiB | 303.6 MiB | entorno principal, posters, UI, audio, iluminación |
| **Total levels** | **88.9 MiB** | **351.6 MiB** | no incluye por separado todo el runtime/wasm |

`Cine` concentra ~85.5% del peso comprimido de levels. `panelBG.png` es compartido por ambas escenas; `cardAvatars.png` es propio del selector. La mayoría del top 50 es exclusiva de `Cine` o entra a través de `PhotoKitCatalog.asset` usado por esa experiencia.

Escenas deshabilitadas: `SetDeGrabacion`, `Pruebas`, `PruebasJoshi/Cine Joshi`, `Cine2` y `PruebasJoshi/Cine`. Sus assets exclusivos no influyen en este build.

## Problemas detectados

1. **Build reciente publicado sin precompresión:** 42.70 MiB transferibles de más frente a Brotli medido.
2. **Texturas dominantes:** 78.5% del peso descomprimido de assets; sprites de 14.1, 12.4 y 12.4 MiB encabezan la lista.
3. **Catálogo de posters/tarjetas de larga cola:** decenas de imágenes de 600×870 y 810×810 acumulan mucho más que cualquier mesh.
4. **Resolución UI sobredimensionada:** seis slides 1924×1084 y varios fondos/logos se usan como sprites sin mipmaps.
5. **Configuración global/perfil divergente:** fallback y Strip Unused Mesh Components no coinciden.
6. **Dos músicas en calidad Vorbis máxima y Decompress On Load.**
7. **246 modelos usados sin Mesh Compression**, aunque su peso total es secundario.
8. **Recursos TMP Examples & Extras entran por `Resources`** aun sin escenas de ejemplo.
9. **Espacio teórico de variantes enorme**, aunque Unity lo reduce con éxito a 1,664 variantes finales.
10. **Mucho contenido demo no usado en Assets:** eleva tamaño del proyecto/importación, pero no el build.

## Recomendaciones

### Primeras 10 acciones por reducción/riesgo

| # | Acción | Impacto | Riesgo | Ahorro estimado | Dificultad |
|---:|---|---|---|---:|---|
| 1 | Generar/publicar Brotli y validar headers del servidor | 🔴 Alto | 🟢 Seguro | **42.70 MiB de transferencia** medidos | Fácil |
| 2 | Build A/B con `Nu_Claro.png` limitado a 1024 | 🔴 Alto | 🟡 Requiere prueba visual | **~2–4 MiB** | Fácil |
| 3 | Limitar `BancoAzteca_Claro/Oscuro` a 1024 y comparar | 🔴 Alto | 🟡 Requiere prueba visual | **~4–7 MiB combinados** | Fácil |
| 4 | Revisar la familia completa de posters 600×870; probar 512×~742 o formato WebGL explícito | 🔴 Alto | 🟡 Requiere prueba visual | **~10–20 MiB** | Media |
| 5 | Revisar diez tarjetas de taquilla 810×810; probar 512 | 🟠 Medio | 🟡 Requiere prueba visual | **~3–6 MiB** | Fácil |
| 6 | Reducir los seis slides 1924×1084 a resolución efectiva de UI | 🟠 Medio | 🟡 Requiere prueba visual | **~3–6 MiB** | Fácil |
| 7 | Revisar `UI_Circle_Faded`, `Clip_Claro`, `arco` y fondos/contenedores grandes | 🟠 Medio | 🟡 Requiere prueba visual | **~3–7 MiB** | Media |
| 8 | Reducir quality de `Fotos.mp3` y `Fondo.mp3` a 0.55–0.7; probar streaming/memoria por separado | 🟠 Medio | 🟡 Requiere escucha | **~1–2 MiB** | Fácil |
| 9 | Retirar/migrar TMP Examples & Extras Resources si ninguna UI depende de ellos | 🟢 Bajo | 🟡 Requiere prueba | **~1 MiB** | Fácil |
| 10 | Probar Mesh Compression en los 20 modelos mayores, no globalmente | 🟠 Medio | 🟡 Requiere prueba visual/colisiones | **~1–3 MiB** | Media |

Los rangos de assets se basan en tamaños reales del BuildReport y en la reducción cuadrática al bajar resolución, corregida de forma conservadora porque el tamaño descomprimido no equivale al `.data`. Solo un build A/B puede convertirlos en ahorro exacto de red.

Ejemplo de recomendación prioritaria:

> 🔴 `Assets/Sprites/Fondos/Nu_Claro.png`  
> Actualmente: 1925×1925, Sprite, max 2048, sin mipmaps, 14.1 MiB descomprimidos reportados.  
> Propuesta posterior: override WebGL max 1024 y comparación visual en el set fotográfico.  
> Estimación conservadora: 2–4 MiB de transferencia.  
> Riesgo: 🟡 requiere comparación visual. Dificultad: Fácil.

## Plan de optimización

| Prioridad | Acción | Peso actual | Peso esperado | Ahorro estimado | Riesgo |
|---:|---|---:|---:|---:|---|
| 1 | Entrega Brotli correcta | 188.48 MiB | 145.78 MiB | 42.70 MiB | 🟢 Seguro |
| 2 | Top 3 sprites (`Nu`, `BancoAzteca`) a 1024 | 38.9 MiB reportados | ~9.7 MiB reportados | 6–11 MiB de red combinados | 🟡 Visual |
| 3 | Posters + tarjetas en batch controlado | 157+ MiB reportados para JPG + tarjetas | 45–70% menor | 13–26 MiB | 🟡 Visual |
| 4 | Slides/fondos UI | ~20–30 MiB reportados | 45–70% menor | 5–10 MiB | 🟡 Visual |
| 5 | Audio largo | 7.4 MiB reportados | 4–6 MiB | 1–2 MiB | 🟡 Auditivo |
| 6 | TMP Examples Resources | ~1 MiB reportado | ~0 | ~1 MiB | 🟡 Referencias |
| 7 | Mesh compression selectiva | 14.7 MiB reportados | 10–13 MiB | 1–3 MiB | 🟡 Visual/colisiones |

### FASE 1 — cambios prácticamente sin riesgo

1. Usar Brotli en el output final y validar headers/MIME en staging.
2. Mantener Development Build, Script Debugging y debug symbols desactivados.
3. Ejecutar la herramienta `Tools > SNEF > Build Size Audit > Export Latest BuildReport` después de cada build.
4. Resolver la discrepancia de ajustes entre global y Build Profile, sin cambiar valores hasta acordar cuál es la fuente de verdad.
5. Confirmar que el hosting cachea `.data.br`/`.wasm.br` y que Data Caching funciona.

Meta de fase 1: **145–146 MiB transferidos**.

### FASE 2 — requiere revisión visual/auditiva

1. Reducir en lotes pequeños los sprites del top, empezando por los tres mayores.
2. Crear un preset WebGL para posters/tarjetas y hacer capturas comparativas en 1080p/móvil.
3. Ajustar los slides de instrucciones a su máxima resolución visible.
4. Probar quality de los dos temas musicales.
5. Aplicar Mesh Compression solo a los modelos mayores que no presenten artefactos.
6. Revisar TMP Examples Resources.

Meta de fase 2: **115–125 MiB transferidos**. Objetivo recomendado: **120 MiB**.

### FASE 3 — cambios estructurales

1. Separar catálogos de posters por zona/sala mediante Addressables o AssetBundles remotos.
2. Cargar posters y contenido promocional bajo demanda desde CDN.
3. Mantener video exclusivamente por URL/CDN y agregar fallback/telemetría.
4. Dividir `Cine` o sus catálogos para que la descarga inicial no contenga todas las marcas/salas.
5. Evaluar un bootstrap pequeño y descarga progresiva con caché.

Meta de fase 3: **95–110 MiB de descarga inicial**. Llegar a 80 MiB requeriría externalizar una fracción sustancial de posters/contenido y no puede prometerse con la evidencia actual.

## Herramienta de auditoría creada

Se añadió `Assets/Editor/SNEFBuildSizeAudit.cs`. Es código exclusivo de Editor y de solo diagnóstico:

- exporta automáticamente cada BuildReport terminado;
- permite reexportar el último reporte desde el menú `Tools > SNEF > Build Size Audit > Export Latest BuildReport`;
- escribe JSON, CSV y Markdown bajo `Docs/SNEF_BuildReports`;
- registra tamaño empaquetado por asset, archivo contenedor, fuente, tipo, import settings, escenas/dependencias y PlayerSettings;
- no cambia importers, assets, escenas, perfiles ni ProjectSettings.

El siguiente build será la referencia exacta para validar los rangos de ahorro con comparaciones A/B.
