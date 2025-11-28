# Cartoon City Example

Imports the cartoon stickman, house, and tree helpers from `@funcdraw/testlib`, then layers them over the local `sky` model that paints a blue gradient background, sun, and a scattering of clouds.

## Setup

```bash
cd examples/cartoon-city
npm install
```

## Run

```bash
npm run play
```

## Test

```bash
node --test art/sky/eval.test.js
node --test art/ground/eval.test.js
node --test art/skyline/eval.test.js
```
