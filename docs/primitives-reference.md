# Primitives Reference

FuncDraw renders graphics by walking the array (or nested arrays) of primitive objects you return from a `graphics` expression. Each primitive is an object with a `type` string and a `data` record containing the fields listed below. Validation and defaults match the logic used by the hosted FuncDraw Player and the `fd-cli` renderer.

## Shared rules
- Coordinates are 2-element arrays `[x, y]` expressed in the same units as your `view` extent.
- `stroke` and `fill` fields take CSS color strings (`#rrggbb`, `rgb()`, etc.). When omitted, defaults described below kick in.
- `width` values are expressed in world units, not pixels; the renderer scales them to the canvas resolution at draw time.

## `line`
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["type", "data"],
  "properties": {
    "type": { "const": "line" },
    "data": {
      "type": "object",
      "required": ["from", "to"],
      "properties": {
        "from": { "$ref": "#/definitions/point" },
        "to": { "$ref": "#/definitions/point" },
        "stroke": { "type": "string", "default": "#38bdf8" },
        "width": { "type": "number", "default": 0.25 },
        "dash": {
          "type": "array",
          "items": { "type": "number", "minimum": 0 }
        }
      }
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
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["type", "data"],
  "properties": {
    "type": { "const": "rect" },
    "data": {
      "type": "object",
      "required": ["position", "size"],
      "properties": {
        "position": { "$ref": "#/definitions/point" },
        "size": { "$ref": "#/definitions/point" },
        "fill": { "type": "string" },
        "stroke": { "type": "string" },
        "width": { "type": "number", "default": 0.25 }
      }
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

## `circle`
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["type", "data"],
  "properties": {
    "type": { "const": "circle" },
    "data": {
      "type": "object",
      "required": ["center", "radius"],
      "properties": {
        "center": { "$ref": "#/definitions/point" },
        "radius": { "type": "number", "exclusiveMinimum": 0 },
        "fill": { "type": "string" },
        "stroke": { "type": "string" },
        "width": { "type": "number", "default": 0.25 }
      }
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

## `polygon`
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["type", "data"],
  "properties": {
    "type": { "const": "polygon" },
    "data": {
      "type": "object",
      "required": ["points"],
      "properties": {
        "points": {
          "type": "array",
          "minItems": 3,
          "items": { "$ref": "#/definitions/point" }
        },
        "fill": { "type": "string" },
        "stroke": { "type": "string" },
        "width": { "type": "number", "default": 0.25 }
      }
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

## `text`
```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "type": "object",
  "required": ["type", "data"],
  "properties": {
    "type": { "const": "text" },
    "data": {
      "type": "object",
      "required": ["position", "text"],
      "properties": {
        "position": { "$ref": "#/definitions/point" },
        "text": { "type": "string" },
        "color": { "type": "string", "default": "#e2e8f0" },
        "fontSize": { "type": "number", "default": 1 },
        "align": {
          "enum": ["left", "center", "right"],
          "default": "left"
        }
      }
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
