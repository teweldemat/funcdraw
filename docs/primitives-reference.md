# Primitives Reference

FuncDraw renders graphics by walking the array (or nested arrays) of primitive objects you return from a `graphics` expression. Each primitive is an object with a `type` string plus the drawing fields listed below (no nested `data` wrapper required). Validation and defaults match the logic used by the hosted FuncDraw Player and the `fd-cli` renderer.

## Shared rules
- Coordinates are 2-element arrays `[x, y]` expressed in the same units as your `view` extent.
- `stroke` and `fill` fields take CSS color strings (`#rrggbb`, `rgb()`, etc.). When omitted, defaults described below kick in.
- `width` values are expressed in world units, not pixels; the renderer scales them to the canvas resolution at draw time.
- Legacy `{ data: { ... } }` wrappers are rejected. Move drawing properties directly onto the primitive for consistent performance.

## `line`
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["type", "from", "to"],
  "properties": {
    "type": { "const": "line" },
    "from": { "$ref": "#/definitions/point" },
    "to": { "$ref": "#/definitions/point" },
    "stroke": { "type": "string", "default": "#38bdf8" },
    "width": { "type": "number", "default": 0.25 },
    "dash": {
      "type": "array",
      "items": { "type": "number", "minimum": 0 }
    }
  },
  "definitions": {
    "point": {
      "type": "array",
      "prefixItems": [
        { "type": "number" },
        { "type": "number" }
      ],
      "items": false,
      "minItems": 2,
      "maxItems": 2
    }
  }
}
```

## `rect`

> **Position anchor**
>
> The `position` field represents the **bottom-left corner** of the rectangle in view-space coordinates. To center a rectangle at `[cx, cy]` you must subtract half the width/height yourself: `position: [cx - w/2, cy - h/2]`.
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["type", "position", "size"],
  "properties": {
    "type": { "const": "rect" },
    "position": { "$ref": "#/definitions/point" },
    "size": { "$ref": "#/definitions/point" },
    "fill": { "type": "string" },
    "stroke": { "type": "string" },
    "width": { "type": "number", "default": 0.25 }
  },
  "definitions": {
    "point": {
      "type": "array",
      "prefixItems": [
        { "type": "number" },
        { "type": "number" }
      ],
      "items": false,
      "minItems": 2,
      "maxItems": 2
    }
  }
}
```

## `circle`
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["type", "center", "radius"],
  "properties": {
    "type": { "const": "circle" },
    "center": { "$ref": "#/definitions/point" },
    "radius": { "type": "number", "exclusiveMinimum": 0 },
    "fill": { "type": "string" },
    "stroke": { "type": "string" },
    "width": { "type": "number", "default": 0.25 }
  },
  "definitions": {
    "point": {
      "type": "array",
      "prefixItems": [
        { "type": "number" },
        { "type": "number" }
      ],
      "items": false,
      "minItems": 2,
      "maxItems": 2
    }
  }
}
```

## `polygon`
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["type", "points"],
  "properties": {
    "type": { "const": "polygon" },
    "points": {
      "type": "array",
      "minItems": 3,
      "items": { "$ref": "#/definitions/point" }
    },
    "fill": { "type": "string" },
    "stroke": { "type": "string" },
    "width": { "type": "number", "default": 0.25 }
  },
  "definitions": {
    "point": {
      "type": "array",
      "prefixItems": [
        { "type": "number" },
        { "type": "number" }
      ],
      "items": false,
      "minItems": 2,
      "maxItems": 2
    }
  }
}
```

## `text`
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["type", "position", "text"],
  "properties": {
    "type": { "const": "text" },
    "position": { "$ref": "#/definitions/point" },
    "text": { "type": "string" },
    "color": { "type": "string", "default": "#e2e8f0" },
    "fontSize": { "type": "number", "default": 1 },
    "align": {
      "enum": ["left", "center", "right"],
      "default": "left"
    }
  },
  "definitions": {
    "point": {
      "type": "array",
      "prefixItems": [
        { "type": "number" },
        { "type": "number" }
      ],
      "items": false,
      "minItems": 2,
      "maxItems": 2
    }
  }
}
```

### Layered output
Returning a list of primitives or a list-of-lists is valid. Each nested list is treated as a layer, preserving draw order. Anything that isn't recognized as one of the five `type` strings is ignored and reported in the player warning sidebar.
