(state) =>
{
  cfg: config;
  mathLib: helpers;
  pickerLib: pickers;
  selectionLib: selection;
  uiLib: ui;
  decorLib: decorations;
  stepperLib: stepper;

  palette: cfg.palette;
  view: cfg.view;

  character: package("@funcdraw/testlib").cartoon.character;
  slider: package("@funcdraw/testlib").ui.slider;
  toggle: package("@funcdraw/testlib").ui.toggle;
  dragdot: package("@funcdraw/testlib").ui.dragdot;

  anchorRaw: if state == null then null else state.anchor;
  anchor:
    if state == null then cfg.defaultAnchor
    else if anchorRaw == null then cfg.defaultAnchor
    else if Len(anchorRaw) > 2 then [anchorRaw[0], anchorRaw[1]]
    else anchorRaw;
  measurements: if state == null then cfg.defaultMeasurements else state.measurements;

  selectedPart: if state == null then cfg.defaultSelectedPart else state.selectedPart;
  hoveredPart: if state == null then null else state.hoveredPart;

  primarySliderState: if state == null then null else state.primarySlider;
  secondarySliderState: if state == null then null else state.secondarySlider;
  sameSidesToggleState: if state == null then null else state.sameSidesToggle;
  bendToggleState: if state == null then null else state.bendToggle;
  sameSides: if state == null then cfg.defaultSameSides else state.sameSides;

  legacyDotState: (legacy) =>
  {
    s: legacy;
    eval
      if s == null then null
      else if Len(s) <= 2 then null
      else
      {
        pointerId: s[2];
        offset: [s[3]??0, s[4]??0];
        hovered: s[5]??false;
      };
  };

  anchorDotState:
    if state == null then null
    else if state.anchorDot != null then state.anchorDot
    else legacyDotState(anchorRaw);

  leftHandEndDotState:
    if state == null then null
    else if state.leftHandEndDot != null then state.leftHandEndDot
    else legacyDotState(state.leftHandEnd);

  rightHandEndDotState:
    if state == null then null
    else if state.rightHandEndDot != null then state.rightHandEndDot
    else legacyDotState(state.rightHandEnd);

  leftLegEndDotState:
    if state == null then null
    else if state.leftLegEndDot != null then state.leftLegEndDot
    else legacyDotState(state.leftLegEnd);

  rightLegEndDotState:
    if state == null then null
    else if state.rightLegEndDot != null then state.rightLegEndDot
    else legacyDotState(state.rightLegEnd);

  geometry: character.skeleton.build(anchor, measurements);
  characterGraphics: character.static(anchor, measurements, palette, character.skins.stick);

  headOffset:
  [
    geometry.measurements.headRadius * math.Cos(geometry.measurements.neckAngle),
    geometry.measurements.headRadius * math.Sin(geometry.measurements.neckAngle)
  ];
  headCenter: [geometry.neck.to[0] + headOffset[0], geometry.neck.to[1] + headOffset[1]];

  hit: mathLib.makeHitTester(cfg.hitRadius);
  pickPart: pickerLib.build({ geometry; headCenter; hit; });

  selectionInfo: selectionLib.describe(selectedPart, geometry.measurements);

  uiBuilt:
    uiLib.build(
      {
        slider;
        toggle;
        view;
        selectedPart;
        sameSides;
        selection: selectionInfo;
        primarySliderState;
        secondarySliderState;
        sameSidesToggleState;
        bendToggleState;
      });

  handleStroke: "#0f172a";
  handleWidth: 0.2;
  handleBaseFill: "#e2e8f0";
  handleActiveFill: "#fbbf24";
  anchorFill: "#f472b6";

  anchorDot:
    dragdot(
      {
        point: anchor;
        radius: cfg.handleRadius;
        fill: anchorFill;
        activeFill: handleActiveFill;
        stroke: handleStroke;
        width: handleWidth;
      },
      anchorDotState);

  leftHandEndDot:
    dragdot(
      {
        point: geometry.leftHand.to;
        radius: cfg.handleRadius;
        fill: handleBaseFill;
        activeFill: handleActiveFill;
        stroke: handleStroke;
        width: handleWidth;
      },
      leftHandEndDotState);

  rightHandEndDot:
    dragdot(
      {
        point: geometry.rightHand.to;
        radius: cfg.handleRadius;
        fill: handleBaseFill;
        activeFill: handleActiveFill;
        stroke: handleStroke;
        width: handleWidth;
      },
      rightHandEndDotState);

  leftLegEndDot:
    dragdot(
      {
        point: geometry.leftLeg.to;
        radius: cfg.handleRadius;
        fill: handleBaseFill;
        activeFill: handleActiveFill;
        stroke: handleStroke;
        width: handleWidth;
      },
      leftLegEndDotState);

  rightLegEndDot:
    dragdot(
      {
        point: geometry.rightLeg.to;
        radius: cfg.handleRadius;
        fill: handleBaseFill;
        activeFill: handleActiveFill;
        stroke: handleStroke;
        width: handleWidth;
      },
      rightLegEndDotState);

  decorBuilt:
    decorLib.build(
      {
        geometry;
        headCenter;
        selectedPart;
        hoveredPart;
      });

  sceneStepper:
    stepperLib.make(
      {
        anchor;
        anchorDotState;
        leftHandEndDotState;
        rightHandEndDotState;
        leftLegEndDotState;
        rightLegEndDotState;
        measurements;
        selectedPart;
        hoveredPart;
        primarySliderState;
        secondarySliderState;
        sameSidesToggleState;
        bendToggleState;
        sameSides;
        geometry;
        pickPart;
        primarySlider: uiBuilt.primarySlider;
        secondarySlider: uiBuilt.secondarySlider;
        sameSidesToggle: uiBuilt.sameSidesToggle;
        bendToggle: uiBuilt.bendToggle;
        anchorDot;
        leftHandEndDot;
        rightHandEndDot;
        leftLegEndDot;
        rightLegEndDot;
      });

  eval
  {
    view;
    graphics:
      [
        characterGraphics,
        decorBuilt.hoverHighlight,
        decorBuilt.highlight,
        anchorDot.graphics,
        leftHandEndDot.graphics,
        rightHandEndDot.graphics,
        leftLegEndDot.graphics,
        rightLegEndDot.graphics,
        uiBuilt.panelBackground,
        uiBuilt.panelTitle,
        uiBuilt.panelHint,
        uiBuilt.panelSelected,
        uiBuilt.sameSidesToggle.graphics,
        if uiBuilt.bendToggle == null then [] else uiBuilt.bendToggle.graphics,
        uiBuilt.primarySlider.graphics,
        if uiBuilt.secondarySlider == null then [] else uiBuilt.secondarySlider.graphics
      ];
    step: sceneStepper;
  };
}
