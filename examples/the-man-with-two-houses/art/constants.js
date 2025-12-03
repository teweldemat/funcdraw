return {
  view: { left: -400, bottom: -300, right: 400, top: 300 },
  fontSize: 12,

  // Duration knobs so eval.js can chain the scenes.
  scene1Duration: 4.5,
  scene2: {
    duration: 3,
    margin: 60,
    handSwing: true
  },
  scene3: {
    doorDuration: 1.5,
    zoomDuration: 3,
    zoomTarget: 1.3,
    primaryDoorOpenLevel: 0
  },

  // Shared pose defaults for the man/house scenes.
  shared: {
    anchor: [0, 26]
  },
  manHouse: {
    anchor: [0, 26],
    insideOffset: [0, 6],
    torso: { height: 16, width: 10 },
    head: { verticalExtent: 9 },
    hands: { left: [-8, 5], right: [8, 5] },
    legOffsets: { left: [0, -26], right: [-10, -26] },
    legs: { lengths: { upper: 16, lower: 15 } }
  }
};
