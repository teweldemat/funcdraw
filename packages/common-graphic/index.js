const car = require('./car');
const wheel = require('./wheel');

module.exports = {
  car,
  wheel,
  default: {
    car,
    wheel
  }
};
