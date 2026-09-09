# build-manifest.json — Inventario estable del build (SNEF 2.0)

> Documento solicitado en el paso 3 del mensaje de integración:
> *"Un `build-manifest.json` al lado del build … con los ids de los avatares, los
> slots del set y los objetos disponibles … Los ids los definen ustedes; solo
> necesitamos que sean estables entre builds."*

Este documento define **qué ids existen hoy en el build** para que la plataforma
de patrocinadores arme sus selectores sola y no pueda elegir algo que el juego no
tiene. Todos los ids están tomados **directamente de los datos actuales del
juego** (assets de Unity), no inventados.

- **Fecha:** 2026-09-06
- **Fuente de datos:**
  - Avatares → `Assets/Scripts/CharacterDatabase.asset`
  - Taquilla (prendas) → `Assets/Scripts/CustomizationCatalog.asset`
  - Taquilla (kits) → `Assets/Scripts/SetFotografia/PhotoKitCatalog.asset`

**Estado por sección:**

| Sección | Estado |
|---|---|
| Avatares | ✅ Definido |
| Objetos de taquilla (prendas + kits) | ✅ Definido |
| Sets de grabación (slots + objetos del set) | ⏳ **Pendiente** — a definir según el número final de patrocinadores |

---

## 1. Avatares

12 avatares disponibles. El patrocinador elige **uno** por su `avatarId`.

| avatarId | Estado |
|---|---|
| `avatar_01` | Disponible |
| `avatar_02` | Disponible |
| `avatar_03` | Disponible |
| `avatar_04` | Disponible |
| `avatar_05` | Disponible |
| `avatar_06` | Disponible |
| `avatar_07` | Disponible |
| `avatar_08` | Disponible |
| `avatar_09` | Disponible |
| `avatar_10` | Disponible |
| `avatar_11` | Disponible |
| `avatar_12` | Disponible |

> Ids estables entre builds. Si se agregan/retiran avatares, se actualiza aquí y
> se regenera el manifiesto.

---

## 2. Objetos de taquilla (tienda)

Catálogo de la tienda dentro del juego. `gratuito: true` = desbloqueado desde el
inicio; el resto se compra con Ditas (`precio`). Los ids son estables.

### 2.1 Sombreros (Hat) — 20

| optionId | precio | gratuito |
|---|---|---|
| `Hat_0` | 0 | ✅ |
| `Hat_1` | 0 | ✅ |
| `Hat_26` | 0 | ✅ |
| `Hat_4` | 150 | — |
| `Hat_5` | 150 | — |
| `Hat_6` | 150 | — |
| `Hat_10` | 200 | — |
| `Hat_11` | 200 | — |
| `Hat_13` | 200 | — |
| `Hat_14` | 200 | — |
| `Hat_15` | 200 | — |
| `Hat_16` | 200 | — |
| `Hat_18` | 200 | — |
| `Hat_24` | 250 | — |
| `Hat_27` | 250 | — |
| `Hat_28` | 250 | — |
| `Hat_29` | 250 | — |
| `Hat_30` | 250 | — |
| `Hat_22` | 300 | — |
| `Hat_25` | 300 | — |

### 2.2 Pantalones (Pants) — 15

| optionId | precio | gratuito |
|---|---|---|
| `Pants_0` | 0 | ✅ |
| `Pants_1` | 50 | — |
| `Pants_2` | 50 | — |
| `Pants_3` | 50 | — |
| `Pants_4` | 50 | — |
| `Pants_5` | 50 | — |
| `Pants_6` | 50 | — |
| `Pants_7` | 50 | — |
| `Pants_8` | 50 | — |
| `Pants_9` | 50 | — |
| `Pants_10` | 50 | — |
| `Pants_11` | 50 | — |
| `Pants_12` | 100 | — |
| `Pants_13` | 100 | — |
| `Pants_14` | 100 | — |

### 2.3 Zapatos (Shoes) — 20

| optionId | precio | gratuito |
|---|---|---|
| `Shoes_0` | 0 | ✅ |
| `Shoes_1` | 150 | — |
| `Shoes_2` | 150 | — |
| `Shoes_3` | 150 | — |
| `Shoes_6` | 150 | — |
| `Shoes_7` | 150 | — |
| `Shoes_8` | 150 | — |
| `Shoes_9` | 200 | — |
| `Shoes_10` | 200 | — |
| `Shoes_11` | 200 | — |
| `Shoes_12` | 200 | — |
| `Shoes_13` | 200 | — |
| `Shoes_14` | 200 | — |
| `Shoes_15` | 200 | — |
| `Shoes_16` | 200 | — |
| `Shoes_17` | 250 | — |
| `Shoes_18` | 250 | — |
| `Shoes_19` | 250 | — |
| `Shoes_20` | 250 | — |
| `Shoes_21` | 250 | — |

### 2.4 Accesorios (Acce) — 14

| optionId | precio | gratuito |
|---|---|---|
| `Acce_0` | 0 | ✅ |
| `Acce_1` | 150 | — |
| `Acce_2` | 150 | — |
| `Acce_3` | 150 | — |
| `Acce_4` | 150 | — |
| `Acce_5` | 200 | — |
| `Acce_6` | 200 | — |
| `Acce_7` | 200 | — |
| `Acce_8` | 250 | — |
| `Acce_9` | 250 | — |
| `Acce_10` | 250 | — |
| `Acce_11` | 250 | — |
| `Acce_12` | 250 | — |
| `Acce_13` | 250 | — |

### 2.5 Kits de foto (set de fotografía) — 5

Cada kit coloca **2 objetos** en el set de fotografía (posiciones fijas
`x = +1` y `x = -1` respecto al ancla del set).

| kitId | precio | gratuito | objetos |
|---|---|---|---|
| `Kit 1` | 0 | ✅ | 2 |
| `Kit 2` | 200 | — | 2 |
| `Kit 3` | 200 | — | 2 |
| `Kit 4` | 200 | — | 2 |
| `Kit 5` | 200 | — | 2 |

> ⚠️ **Recomendación de ids:** los `kitId` actuales tienen espacio (`"Kit 1"`).
> Para que sean estables y seguros en JSON/URLs conviene normalizarlos a
> `kit_01`, `kit_02`, … en una próxima pasada. Aquí se listan tal cual están hoy
> para no romper referencias existentes.

---

## 3. Sets de grabación (⏳ pendiente)

Esta sección **queda por definir en función del número final de patrocinadores**
y del diseño definitivo de la sala genérica instanciable (paso 2 del mensaje).

Lo que ya existe como base en el juego: el **set de fotografía** admite **2
objetos** por kit, colocados en dos posiciones fijas (`x = +1` y `x = -1`). Eso
nos da un punto de partida de **2 slots**, pero el número y nombre definitivos de
los slots del set de grabación de patrocinadores aún no están cerrados.

Falta acordar, cuando se sepa el total de patrocinadores:

- **Slots del set** (`slot`): nombres estables tipo `floor_center`, `floor_left`,
  `floor_right`, `wall_back`, etc. — cuántos y cuáles.
- **Objetos disponibles** (`objectId`): catálogo de objetos que un patrocinador
  puede colocar en cada slot (ej. `camera_tripod_01`), con sus ids estables.
- **Compatibilidad slot ↔ objeto:** qué objetos pueden ir en qué slots.

Propuesta de convención (a validar):

| Campo | Convención sugerida | Ejemplo |
|---|---|---|
| `slot` | `zona_posicion` en snake_case | `floor_center` |
| `objectId` | `objeto_##` en snake_case | `camera_tripod_01` |

---

## 4. build-manifest.json

Estructura propuesta con los datos definidos + los pendientes marcados. Es el
archivo que iría **al lado del build**.

```json
{
  "manifestVersion": "1.0.0",
  "generatedAt": "2026-09-06",
  "notes": "Ids estables entre builds. La sección 'set' está pendiente de definir segun el numero final de patrocinadores.",

  "avatars": [
    { "avatarId": "avatar_01" },
    { "avatarId": "avatar_02" },
    { "avatarId": "avatar_03" },
    { "avatarId": "avatar_04" },
    { "avatarId": "avatar_05" },
    { "avatarId": "avatar_06" },
    { "avatarId": "avatar_07" },
    { "avatarId": "avatar_08" },
    { "avatarId": "avatar_09" },
    { "avatarId": "avatar_10" },
    { "avatarId": "avatar_11" },
    { "avatarId": "avatar_12" }
  ],

  "shop": {
    "hat": [
      { "optionId": "Hat_0",  "precio": 0,   "gratuito": true },
      { "optionId": "Hat_1",  "precio": 0,   "gratuito": true },
      { "optionId": "Hat_26", "precio": 0,   "gratuito": true },
      { "optionId": "Hat_4",  "precio": 150, "gratuito": false },
      { "optionId": "Hat_5",  "precio": 150, "gratuito": false },
      { "optionId": "Hat_6",  "precio": 150, "gratuito": false },
      { "optionId": "Hat_10", "precio": 200, "gratuito": false },
      { "optionId": "Hat_11", "precio": 200, "gratuito": false },
      { "optionId": "Hat_13", "precio": 200, "gratuito": false },
      { "optionId": "Hat_14", "precio": 200, "gratuito": false },
      { "optionId": "Hat_15", "precio": 200, "gratuito": false },
      { "optionId": "Hat_16", "precio": 200, "gratuito": false },
      { "optionId": "Hat_18", "precio": 200, "gratuito": false },
      { "optionId": "Hat_24", "precio": 250, "gratuito": false },
      { "optionId": "Hat_27", "precio": 250, "gratuito": false },
      { "optionId": "Hat_28", "precio": 250, "gratuito": false },
      { "optionId": "Hat_29", "precio": 250, "gratuito": false },
      { "optionId": "Hat_30", "precio": 250, "gratuito": false },
      { "optionId": "Hat_22", "precio": 300, "gratuito": false },
      { "optionId": "Hat_25", "precio": 300, "gratuito": false }
    ],
    "pants": [
      { "optionId": "Pants_0",  "precio": 0,   "gratuito": true },
      { "optionId": "Pants_1",  "precio": 50,  "gratuito": false },
      { "optionId": "Pants_2",  "precio": 50,  "gratuito": false },
      { "optionId": "Pants_3",  "precio": 50,  "gratuito": false },
      { "optionId": "Pants_4",  "precio": 50,  "gratuito": false },
      { "optionId": "Pants_5",  "precio": 50,  "gratuito": false },
      { "optionId": "Pants_6",  "precio": 50,  "gratuito": false },
      { "optionId": "Pants_7",  "precio": 50,  "gratuito": false },
      { "optionId": "Pants_8",  "precio": 50,  "gratuito": false },
      { "optionId": "Pants_9",  "precio": 50,  "gratuito": false },
      { "optionId": "Pants_10", "precio": 50,  "gratuito": false },
      { "optionId": "Pants_11", "precio": 50,  "gratuito": false },
      { "optionId": "Pants_12", "precio": 100, "gratuito": false },
      { "optionId": "Pants_13", "precio": 100, "gratuito": false },
      { "optionId": "Pants_14", "precio": 100, "gratuito": false }
    ],
    "shoes": [
      { "optionId": "Shoes_0",  "precio": 0,   "gratuito": true },
      { "optionId": "Shoes_1",  "precio": 150, "gratuito": false },
      { "optionId": "Shoes_2",  "precio": 150, "gratuito": false },
      { "optionId": "Shoes_3",  "precio": 150, "gratuito": false },
      { "optionId": "Shoes_6",  "precio": 150, "gratuito": false },
      { "optionId": "Shoes_7",  "precio": 150, "gratuito": false },
      { "optionId": "Shoes_8",  "precio": 150, "gratuito": false },
      { "optionId": "Shoes_9",  "precio": 200, "gratuito": false },
      { "optionId": "Shoes_10", "precio": 200, "gratuito": false },
      { "optionId": "Shoes_11", "precio": 200, "gratuito": false },
      { "optionId": "Shoes_12", "precio": 200, "gratuito": false },
      { "optionId": "Shoes_13", "precio": 200, "gratuito": false },
      { "optionId": "Shoes_14", "precio": 200, "gratuito": false },
      { "optionId": "Shoes_15", "precio": 200, "gratuito": false },
      { "optionId": "Shoes_16", "precio": 200, "gratuito": false },
      { "optionId": "Shoes_17", "precio": 250, "gratuito": false },
      { "optionId": "Shoes_18", "precio": 250, "gratuito": false },
      { "optionId": "Shoes_19", "precio": 250, "gratuito": false },
      { "optionId": "Shoes_20", "precio": 250, "gratuito": false },
      { "optionId": "Shoes_21", "precio": 250, "gratuito": false }
    ],
    "accesories": [
      { "optionId": "Acce_0",  "precio": 0,   "gratuito": true },
      { "optionId": "Acce_1",  "precio": 150, "gratuito": false },
      { "optionId": "Acce_2",  "precio": 150, "gratuito": false },
      { "optionId": "Acce_3",  "precio": 150, "gratuito": false },
      { "optionId": "Acce_4",  "precio": 150, "gratuito": false },
      { "optionId": "Acce_5",  "precio": 200, "gratuito": false },
      { "optionId": "Acce_6",  "precio": 200, "gratuito": false },
      { "optionId": "Acce_7",  "precio": 200, "gratuito": false },
      { "optionId": "Acce_8",  "precio": 250, "gratuito": false },
      { "optionId": "Acce_9",  "precio": 250, "gratuito": false },
      { "optionId": "Acce_10", "precio": 250, "gratuito": false },
      { "optionId": "Acce_11", "precio": 250, "gratuito": false },
      { "optionId": "Acce_12", "precio": 250, "gratuito": false },
      { "optionId": "Acce_13", "precio": 250, "gratuito": false }
    ],
    "kits": [
      { "kitId": "Kit 1", "precio": 0,   "gratuito": true,  "objetos": 2 },
      { "kitId": "Kit 2", "precio": 200, "gratuito": false, "objetos": 2 },
      { "kitId": "Kit 3", "precio": 200, "gratuito": false, "objetos": 2 },
      { "kitId": "Kit 4", "precio": 200, "gratuito": false, "objetos": 2 },
      { "kitId": "Kit 5", "precio": 200, "gratuito": false, "objetos": 2 }
    ]
  },

  "set": {
    "_estado": "PENDIENTE: a definir segun el numero final de patrocinadores",
    "slots": [],
    "objetosDisponibles": []
  }
}
```

---

## 5. Notas y siguientes pasos

- **Ids estables:** avatares, prendas y kits ya tienen ids fijos que no deben
  cambiar entre builds. Si algo se agrega o retira, se actualiza aquí y se
  regenera el JSON.
- **Kits con espacio en el id:** normalizar `"Kit 1"` → `kit_01` en una próxima
  iteración (ver nota en la sección 2.5).
- **Sets de grabación:** es lo único pendiente. Se cerrará cuando se confirme el
  número final de patrocinadores y el diseño de la sala genérica instanciable.
- **Totales actuales:** 12 avatares · 20 sombreros · 15 pantalones · 20 zapatos ·
  14 accesorios · 5 kits.
