const body = [
  {
    type: 'rect',
    data: {
      position: [-6, -1.5],
      size: [12, 3],
      fill: '#38bdf8',
      stroke: '#0f172a',
      width: 0.25
    }
  },
  {
    type: 'rect',
    data: {
      position: [-4.5, -3],
      size: [9, 1.5],
      fill: '#1e293b'
    }
  },
  {
    type: 'circle',
    data: {
      center: [-4, 1.6],
      radius: 1.4,
      fill: '#94a3b8'
    }
  },
  {
    type: 'circle',
    data: {
      center: [4, 1.6],
      radius: 1.4,
      fill: '#94a3b8'
    }
  },
  {
    type: 'circle',
    data: {
      center: [-4, 1.6],
      radius: 0.9,
      fill: '#0f172a'
    }
  },
  {
    type: 'circle',
    data: {
      center: [4, 1.6],
      radius: 0.9,
      fill: '#0f172a'
    }
  }
];

const car = [
  {
    type: 'rect',
    data: {
      position: [-5, -1],
      size: [10, 2],
      fill: '#0f172a'
    }
  },
  ...body
];

module.exports = {
  car,
  default: {
    car
  }
};
