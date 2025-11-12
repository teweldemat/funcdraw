const sun = require('./sun');
const hill = require('./hill');
const meadow = require('./meadow');

module.exports = {
  sun,
  hill,
  meadow,
  default: {
    sun,
    hill,
    meadow
  }
};
