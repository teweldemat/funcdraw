const body = require('./body');
const car = require('./car');

module.exports = {
  body,
  car,
  default: {
    body,
    car
  }
};
