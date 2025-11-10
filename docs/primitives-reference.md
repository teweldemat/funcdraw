# Primitives Reference

FuncDraw renders graphics by walking the array (or nested arrays) of primitive objects you return from a `graphics` expression. Each primitive is an object with a `type` string and a `data` record containing the fields listed below. Validation and defaults match the logic used by the hosted FuncDraw Player and the `fd-cli` renderer.

## Shared rules
- Coordinates are 2-element arrays `[x, y]` expressed in the same units as your `view` extent.
- `stroke` and `fill` fields take CSS color strings (`#rrggbb`, `rgb()`, etc.). When omitted, defaults described below kick in.
- `width` values are expressed in world units, not pixels; the renderer scales them to the canvas resolution at draw time.

## `line`
| Field | Type | Notes |
| --- | --- | --- |
| `from` | point | Required start coordinate. |
| `to` | point | Required end coordinate. |
| `stroke` | string | Defaults to `#38bdf8` if missing. |
| `width` | number | Defaults to `0.25`. |
| `dash` | number[] | Optional stroke dash pattern; ignored unless every entry is `>= 0`. |

## `rect`
| Field | Type | Notes |
| --- | --- | --- |
| `position` | point | Required lower-left corner. |
| `size` | point | Required `[width, height]`. |
| `fill` | string | Optional; no fill when omitted. |
| `stroke` | string | Optional outline color. |
| `width` | number | Stroke width; defaults to `0.25`. |

## `circle`
| Field | Type | Notes |
| --- | --- | --- |
| `center` | point | Required. |
| `radius` | number | Required positive value. |
| `fill` | string | Optional interior color. |
| `stroke` | string | Optional outline color. |
| `width` | number | Stroke width (default `0.25`). |

## `polygon`
| Field | Type | Notes |
| --- | --- | --- |
| `points` | point[] | Required list of ≥3 points; renderer closes the path automatically. |
| `fill` | string | Optional. |
| `stroke` | string | Optional. |
| `width` | number | Stroke width (default `0.25`). |

## `text`
| Field | Type | Notes |
| --- | --- | --- |
| `position` | point | Required baseline anchor. |
| `text` | string | Required content. |
| `color` | string | Defaults to `#e2e8f0`. |
| `fontSize` | number | Defaults to `1` world unit (scaled in the player so it never renders smaller than 12px). |
| `align` | `'left' | 'center' | 'right'` | Defaults to `'left'`; `'center'` maps to `canvas.textAlign = 'center'`, `'right'` to `'right'`. |

### Layered output
Returning a list of primitives or a list-of-lists is valid. Each nested list is treated as a layer, preserving draw order. Anything that isn't recognized as one of the five `type` strings is ignored and reported in the player warning sidebar.
