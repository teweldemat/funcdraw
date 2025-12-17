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

  anchor: if state == null then cfg.defaultAnchor else state.anchor;
  measurements: if state == null then cfg.defaultMeasurements else state.measurements;

  selectedPart: if state == null then cfg.defaultSelectedPart else state.selectedPart;
  hoveredPart: if state == null then null else state.hoveredPart;
  dragging: if state == null then null else state.dragging;

  primarySliderState: if state == null then null else state.primarySlider;
  secondarySliderState: if state == null then null else state.secondarySlider;
  sameSidesToggleState: if state == null then null else state.sameSidesToggle;
  bendToggleState: if state == null then null else state.bendToggle;
  sameSides: if state == null then cfg.defaultSameSides else state.sameSides;

  geometry: character.skeleton.build(anchor, measurements);
  characterGraphics: character.static(anchor, measurements, palette, character.skins.stick);

  headOffset:
  [
    geometry.measurements.headRadius * math.Cos(geometry.measurements.neckAngle),
    geometry.measurements.headRadius * math.Sin(geometry.measurements.neckAngle)
  ];
  headCenter: [geometry.neck.to[0] + headOffset[0], geometry.neck.to[1] + headOffset[1]];

  hit: mathLib.makeHitTester(cfg.hitRadius);
  picker: pickerLib.build({ geometry; headCenter; anchor; hit; handleRadius: cfg.handleRadius; });

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

  decorBuilt:
    decorLib.build(
      {
        geometry;
        headCenter;
        selectedPart;
        hoveredPart;
        anchor;
        dragging;
        handleRadius: cfg.handleRadius;
      });

  sceneStepper:
    stepperLib.make(
      {
        anchor;
        measurements;
        selectedPart;
        hoveredPart;
        dragging;
        primarySliderState;
        secondarySliderState;
        sameSidesToggleState;
        bendToggleState;
        sameSides;
        geometry;
        pickPart: picker.pickPart;
        pickHandle: picker.pickHandle;
        primarySlider: uiBuilt.primarySlider;
        secondarySlider: uiBuilt.secondarySlider;
        sameSidesToggle: uiBuilt.sameSidesToggle;
        bendToggle: uiBuilt.bendToggle;
      });

  eval
  {
    view;
    graphics:
      [
        characterGraphics,
        decorBuilt.hoverHighlight,
        decorBuilt.highlight,
        decorBuilt.handles,
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
