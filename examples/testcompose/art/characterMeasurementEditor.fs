(state) =>
{
  palette:
  {
    body: "#38bdf8";
    limb: "#bd8c31ff";
  };

  character: package("@funcdraw/testlib").cartoon.character;
  slider: package("@funcdraw/testlib").ui.slider;
  toggle: package("@funcdraw/testlib").ui.toggle;

  view:
  {
    left: -40;
    bottom: -30;
    right: 40;
    top: 30;
  };

  anchor: if state == null then [-20, -15] else state.anchor;

  measurements:
    if state == null then
    {
      leftHand: {};
      rightHand: {};
      leftLeg: {};
      rightLeg: {};
    }
    else state.measurements;

  selectedPart: if state == null then "head" else state.selectedPart;
  hoveredPart: if state == null then null else state.hoveredPart;
  dragging: if state == null then null else state.dragging;

  primarySliderState: if state == null then null else state.primarySlider;
  secondarySliderState: if state == null then null else state.secondarySlider;
  sameSidesToggleState: if state == null then null else state.sameSidesToggle;
  bendToggleState: if state == null then null else state.bendToggle;
  sameSides: if state == null then false else state.sameSides;

  geometry: character.skeleton.build(anchor, measurements);
  characterGraphics: character.static(anchor, measurements, palette, character.skins.stick);

  headOffset:
  [
    geometry.measurements.headRadius * math.Cos(geometry.measurements.neckAngle),
    geometry.measurements.headRadius * math.Sin(geometry.measurements.neckAngle)
  ];
  headCenter: [geometry.neck.to[0] + headOffset[0], geometry.neck.to[1] + headOffset[1]];

  clamp: (x, lo, hi) =>
  {
    eval if x < lo then lo else if x > hi then hi else x;
  };
  dot: (a, b) => a[0] * b[0] + a[1] * b[1];
  dist2: (a, b) => (a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]);

  segmentDist2: (p, a, b) =>
  {
    ab: [b[0] - a[0], b[1] - a[1]];
    ap: [p[0] - a[0], p[1] - a[1]];
    abLen2: ab[0] * ab[0] + ab[1] * ab[1];
    tRaw: if abLen2 == 0 then 0 else dot(ap, ab) / abLen2;
    ratio: clamp(tRaw, 0, 1);
    closest: [a[0] + ab[0] * ratio, a[1] + ab[1] * ratio];
    eval dist2(p, closest);
  };

  hitRadius: 1.4;
  hitRadius2: hitRadius * hitRadius;

  hitSegment: (point, a, b) => segmentDist2(point, a, b) <= hitRadius2;
  hitCircle: (point, center, radius) =>
    dist2(point, center) <= (radius + hitRadius) * (radius + hitRadius);

  pickPart: (eventPoint) =>
  {
    p: [eventPoint.x, eventPoint.y];
    eval
      if hitCircle(p, headCenter, geometry.measurements.headRadius) then "head"
      else if hitSegment(p, geometry.neck.from, geometry.neck.to) then "neck"
      else if hitSegment(p, geometry.body.from, geometry.body.to) then "body"
      else if hitSegment(p, geometry.leftHand.from, geometry.leftHand.joint) or hitSegment(p, geometry.leftHand.joint, geometry.leftHand.to) then "leftArm"
      else if hitSegment(p, geometry.rightHand.from, geometry.rightHand.joint) or hitSegment(p, geometry.rightHand.joint, geometry.rightHand.to) then "rightArm"
      else if hitSegment(p, geometry.leftLeg.from, geometry.leftLeg.joint) or hitSegment(p, geometry.leftLeg.joint, geometry.leftLeg.to) then "leftLeg"
      else if hitSegment(p, geometry.rightLeg.from, geometry.rightLeg.joint) or hitSegment(p, geometry.rightLeg.joint, geometry.rightLeg.to) then "rightLeg"
      else if hitSegment(p, geometry.leftHandAttachment, geometry.rightHandAttachment) then "shoulders"
      else if hitSegment(p, geometry.leftLegAttachment, geometry.rightLegAttachment) then "thighs"
      else null;
  };

  handleRadius: 0.9;
  pickHandle: (eventPoint) =>
  {
    p: [eventPoint.x, eventPoint.y];
    eval
      if hitCircle(p, anchor, handleRadius) then "anchor"
      else if hitCircle(p, geometry.leftHand.to, handleRadius) then "leftHandEnd"
      else if hitCircle(p, geometry.rightHand.to, handleRadius) then "rightHandEnd"
      else if hitCircle(p, geometry.leftLeg.to, handleRadius) then "leftLegEnd"
      else if hitCircle(p, geometry.rightLeg.to, handleRadius) then "rightLegEnd"
      else null;
  };

  panel:
  {
    left: 4;
    bottom: view.bottom + 2;
    right: view.right - 2;
    top: view.top - 2;
  };
  panelPadding: 2;
  uiX: panel.left + panelPadding;
  uiWidth: panel.right - panel.left - panelPadding * 2;
  sliderHeight: 2.3;
  sliderGap: 8;
  sliderY1: panel.top - 16;
  sliderY2: sliderY1 - sliderGap;
  toggleHeight: 2.3;
  toggleY: sliderY1 + sliderHeight + 3;
  bendToggleY: sliderY2 - toggleHeight - 3;

  singleSliderRange:
  {
    head: { min: 1; max: 6; };
    neck: { min: 0; max: 5; };
    body: { min: 8; max: 24; };
    shoulders: { min: 0; max: 6; };
    thighs: { min: 0; max: 4; };
  };

  limbRange:
  {
    min: 2;
    max: 20;
  };

  primaryValue:
    if selectedPart == "head" then geometry.measurements.headRadius
    else if selectedPart == "neck" then geometry.measurements.neckLength
    else if selectedPart == "body" then geometry.measurements.height
    else if selectedPart == "shoulders" then geometry.measurements.shoulderWidth
    else if selectedPart == "thighs" then geometry.measurements.thighWidth
    else if selectedPart == "leftArm" then geometry.measurements.leftHand.upper
    else if selectedPart == "rightArm" then geometry.measurements.rightHand.upper
    else if selectedPart == "leftLeg" then geometry.measurements.leftLeg.upper
    else if selectedPart == "rightLeg" then geometry.measurements.rightLeg.upper
    else error("expected selectedPart");

  secondaryValue:
    if selectedPart == "leftArm" then geometry.measurements.leftHand.lower
    else if selectedPart == "rightArm" then geometry.measurements.rightHand.lower
    else if selectedPart == "leftLeg" then geometry.measurements.leftLeg.lower
    else if selectedPart == "rightLeg" then geometry.measurements.rightLeg.lower
    else null;

  hasSecondary:
    selectedPart == "leftArm" or
    selectedPart == "rightArm" or
    selectedPart == "leftLeg" or
    selectedPart == "rightLeg";

  primaryRange:
    if hasSecondary then limbRange
    else if selectedPart == "head" then singleSliderRange.head
    else if selectedPart == "neck" then singleSliderRange.neck
    else if selectedPart == "body" then singleSliderRange.body
    else if selectedPart == "shoulders" then singleSliderRange.shoulders
    else if selectedPart == "thighs" then singleSliderRange.thighs
    else error("expected selectedPart with range");

  secondaryRange: if hasSecondary then limbRange else null;

  primaryLabel:
    if selectedPart == "head" then "headRadius: " + primaryValue
    else if selectedPart == "neck" then "neckLength: " + primaryValue
    else if selectedPart == "body" then "height: " + primaryValue
    else if selectedPart == "shoulders" then "shoulderWidth: " + primaryValue
    else if selectedPart == "thighs" then "thighWidth: " + primaryValue
    else if selectedPart == "leftArm" then "left arm upper: " + primaryValue
    else if selectedPart == "rightArm" then "right arm upper: " + primaryValue
    else if selectedPart == "leftLeg" then "left leg upper: " + primaryValue
    else if selectedPart == "rightLeg" then "right leg upper: " + primaryValue
    else error("expected selectedPart label");

  secondaryLabel:
    if selectedPart == "leftArm" then "left arm lower: " + secondaryValue
    else if selectedPart == "rightArm" then "right arm lower: " + secondaryValue
    else if selectedPart == "leftLeg" then "left leg lower: " + secondaryValue
    else if selectedPart == "rightLeg" then "right leg lower: " + secondaryValue
    else null;

  primarySlider:
    slider(
      {
        position: [uiX, sliderY1];
        size: [uiWidth, sliderHeight];
        min: primaryRange.min;
        max: primaryRange.max;
        value: primaryValue;
        label: primaryLabel;
      },
      primarySliderState);

  secondarySlider:
    if !hasSecondary then null else
      slider(
        {
          position: [uiX, sliderY2];
          size: [uiWidth, sliderHeight];
          min: secondaryRange.min;
          max: secondaryRange.max;
          value: secondaryValue;
          label: secondaryLabel;
        },
        secondarySliderState);

  sameSidesToggle:
    toggle(
      {
        position: [uiX, toggleY];
        size: [uiWidth, toggleHeight];
        value: sameSides;
        label: "same left/right";
      },
      sameSidesToggleState);

  bendFlipped:
    if !hasSecondary then false
    else if selectedPart == "leftArm" then geometry.measurements.leftHand.sign != -1
    else if selectedPart == "rightArm" then geometry.measurements.rightHand.sign != 1
    else if selectedPart == "leftLeg" then geometry.measurements.leftLeg.sign != -1
    else if selectedPart == "rightLeg" then geometry.measurements.rightLeg.sign != 1
    else error("expected limb selectedPart");

  bendToggle:
    if !hasSecondary then null else
      toggle(
        {
          position: [uiX, bendToggleY];
          size: [uiWidth, toggleHeight];
          value: bendFlipped;
          label: "flip bend";
        },
        bendToggleState);

  highlightStroke: "#fbbf24";
  highlightWidth: 0.9;
  selectedHover: hoveredPart == selectedPart;
  selectedHighlightWidth: if selectedHover then highlightWidth + 0.25 else highlightWidth;
  selectedRadiusPad: if selectedHover then 1.0 else 0.6;

  highlight:
    if selectedPart == "head" then
    [
      { type: "circle"; center: headCenter; radius: geometry.measurements.headRadius + selectedRadiusPad; stroke: highlightStroke; width: selectedHighlightWidth; }
    ]
    else if selectedPart == "neck" then
    [
      { type: "line"; from: geometry.neck.from; to: geometry.neck.to; stroke: highlightStroke; width: selectedHighlightWidth; }
    ]
    else if selectedPart == "body" then
    [
      { type: "line"; from: geometry.body.from; to: geometry.body.to; stroke: highlightStroke; width: selectedHighlightWidth; }
    ]
    else if selectedPart == "shoulders" then
    [
      { type: "line"; from: geometry.leftHandAttachment; to: geometry.rightHandAttachment; stroke: highlightStroke; width: selectedHighlightWidth; }
    ]
    else if selectedPart == "thighs" then
    [
      { type: "line"; from: geometry.leftLegAttachment; to: geometry.rightLegAttachment; stroke: highlightStroke; width: selectedHighlightWidth; }
    ]
    else if selectedPart == "leftArm" then
    [
      { type: "line"; from: geometry.leftHand.from; to: geometry.leftHand.joint; stroke: highlightStroke; width: selectedHighlightWidth; },
      { type: "line"; from: geometry.leftHand.joint; to: geometry.leftHand.to; stroke: highlightStroke; width: selectedHighlightWidth; }
    ]
    else if selectedPart == "rightArm" then
    [
      { type: "line"; from: geometry.rightHand.from; to: geometry.rightHand.joint; stroke: highlightStroke; width: selectedHighlightWidth; },
      { type: "line"; from: geometry.rightHand.joint; to: geometry.rightHand.to; stroke: highlightStroke; width: selectedHighlightWidth; }
    ]
    else if selectedPart == "leftLeg" then
    [
      { type: "line"; from: geometry.leftLeg.from; to: geometry.leftLeg.joint; stroke: highlightStroke; width: selectedHighlightWidth; },
      { type: "line"; from: geometry.leftLeg.joint; to: geometry.leftLeg.to; stroke: highlightStroke; width: selectedHighlightWidth; }
    ]
    else if selectedPart == "rightLeg" then
    [
      { type: "line"; from: geometry.rightLeg.from; to: geometry.rightLeg.joint; stroke: highlightStroke; width: selectedHighlightWidth; },
      { type: "line"; from: geometry.rightLeg.joint; to: geometry.rightLeg.to; stroke: highlightStroke; width: selectedHighlightWidth; }
    ]
    else error("expected selectedPart highlight");

  hoverStroke: "#22c55e";
  hoveredOther: hoveredPart != null and hoveredPart != selectedPart;
  hoverWidth: if hoveredOther then 0.9 else 0.5;
  hoverRadiusPad: if hoveredOther then 1.4 else 0.8;

  hoverHighlight:
    if hoveredPart == null or hoveredPart == selectedPart then []
    else if hoveredPart == "head" then
    [
      { type: "circle"; center: headCenter; radius: geometry.measurements.headRadius + hoverRadiusPad; stroke: hoverStroke; width: hoverWidth; }
    ]
    else if hoveredPart == "neck" then
    [
      { type: "line"; from: geometry.neck.from; to: geometry.neck.to; stroke: hoverStroke; width: hoverWidth; }
    ]
    else if hoveredPart == "body" then
    [
      { type: "line"; from: geometry.body.from; to: geometry.body.to; stroke: hoverStroke; width: hoverWidth; }
    ]
    else if hoveredPart == "shoulders" then
    [
      { type: "line"; from: geometry.leftHandAttachment; to: geometry.rightHandAttachment; stroke: hoverStroke; width: hoverWidth; }
    ]
    else if hoveredPart == "thighs" then
    [
      { type: "line"; from: geometry.leftLegAttachment; to: geometry.rightLegAttachment; stroke: hoverStroke; width: hoverWidth; }
    ]
    else if hoveredPart == "leftArm" then
    [
      { type: "line"; from: geometry.leftHand.from; to: geometry.leftHand.joint; stroke: hoverStroke; width: hoverWidth; },
      { type: "line"; from: geometry.leftHand.joint; to: geometry.leftHand.to; stroke: hoverStroke; width: hoverWidth; }
    ]
    else if hoveredPart == "rightArm" then
    [
      { type: "line"; from: geometry.rightHand.from; to: geometry.rightHand.joint; stroke: hoverStroke; width: hoverWidth; },
      { type: "line"; from: geometry.rightHand.joint; to: geometry.rightHand.to; stroke: hoverStroke; width: hoverWidth; }
    ]
    else if hoveredPart == "leftLeg" then
    [
      { type: "line"; from: geometry.leftLeg.from; to: geometry.leftLeg.joint; stroke: hoverStroke; width: hoverWidth; },
      { type: "line"; from: geometry.leftLeg.joint; to: geometry.leftLeg.to; stroke: hoverStroke; width: hoverWidth; }
    ]
    else if hoveredPart == "rightLeg" then
    [
      { type: "line"; from: geometry.rightLeg.from; to: geometry.rightLeg.joint; stroke: hoverStroke; width: hoverWidth; },
      { type: "line"; from: geometry.rightLeg.joint; to: geometry.rightLeg.to; stroke: hoverStroke; width: hoverWidth; }
    ]
    else error("expected hoveredPart");

  panelBackground:
  {
    type: "rect";
    position: [panel.left, panel.bottom];
    size: [panel.right - panel.left, panel.top - panel.bottom];
    fill: "#0b1220";
    stroke: "#1e293b";
    width: 0.35;
  };

  panelTitle:
  {
    type: "text";
    text: "Character measurement editor";
    position: [uiX, panel.top - 2];
    fontSize: 1.6;
    color: "#94a3b8";
  };

  panelHint:
  {
    type: "text";
    text: "Click a part, drag sliders or endpoints";
    position: [uiX, panel.top - 4.4];
    fontSize: 1.2;
    color: "#64748b";
  };

  panelSelected:
  {
    type: "text";
    text: "selected: " + selectedPart + " | same: " + sameSides;
    position: [uiX, panel.top - 6.6];
    fontSize: 1.2;
    color: "#94a3b8";
  };

  draggingKind: if dragging == null then null else dragging.kind;
  handleStroke: "#0f172a";
  handleWidth: 0.2;
  handleBaseFill: "#e2e8f0";
  handleActiveFill: "#fbbf24";
  anchorFill: "#f472b6";
  handle: (center, kind, baseFill) =>
  {
    active: draggingKind == kind;
    eval
    {
      type: "circle";
      center;
      radius: if active then handleRadius * 1.4 else handleRadius;
      fill: if active then handleActiveFill else baseFill;
      stroke: handleStroke;
      width: if active then handleWidth * 2 else handleWidth;
    };
  };

  handles:
  [
    handle(anchor, "anchor", anchorFill),
    handle(geometry.leftHand.to, "leftHandEnd", handleBaseFill),
    handle(geometry.rightHand.to, "rightHandEnd", handleBaseFill),
    handle(geometry.leftLeg.to, "leftLegEnd", handleBaseFill),
    handle(geometry.rightLeg.to, "rightLegEnd", handleBaseFill)
  ];

  stepper: (event) =>
  {
    primaryStep: primarySlider.step(event);
    secondaryStep: if secondarySlider == null then null else secondarySlider.step(event);
    toggleStep: sameSidesToggle.step(event);
    bendStep: if bendToggle == null then null else bendToggle.step(event);

    primaryChanged: primaryStep != null and Len(primaryStep.events) > 0;
    secondaryChanged: secondaryStep != null and Len(secondaryStep.events) > 0;
    toggleChanged: toggleStep != null and Len(toggleStep.events) > 0;
    bendChanged: bendStep != null and Len(bendStep.events) > 0;

    nextSameSides: if toggleChanged then toggleStep.events[0].value else sameSides;

    nextMeasurements:
      if selectedPart == "head" then
        if primaryChanged then measurements + { headRadius: primaryStep.events[0].value; } else measurements
      else if selectedPart == "neck" then
        if primaryChanged then measurements + { neckLength: primaryStep.events[0].value; } else measurements
      else if selectedPart == "body" then
        if primaryChanged then measurements + { height: primaryStep.events[0].value; } else measurements
      else if selectedPart == "shoulders" then
        if primaryChanged then measurements + { shoulderWidth: primaryStep.events[0].value; } else measurements
      else if selectedPart == "thighs" then
        if primaryChanged then measurements + { thighWidth: primaryStep.events[0].value; } else measurements
      else if selectedPart == "leftArm" then
      {
        m1:
          if !primaryChanged then measurements
          else if sameSides then
            measurements
            + { leftHand: measurements.leftHand + { upper: primaryStep.events[0].value; }; }
            + { rightHand: measurements.rightHand + { upper: primaryStep.events[0].value; }; }
          else measurements + { leftHand: measurements.leftHand + { upper: primaryStep.events[0].value; }; };
        m2:
          if !secondaryChanged then m1
          else if sameSides then
            m1
            + { leftHand: m1.leftHand + { lower: secondaryStep.events[0].value; }; }
            + { rightHand: m1.rightHand + { lower: secondaryStep.events[0].value; }; }
          else m1 + { leftHand: m1.leftHand + { lower: secondaryStep.events[0].value; }; };
        eval m2;
      }
      else if selectedPart == "rightArm" then
      {
        m1:
          if !primaryChanged then measurements
          else if sameSides then
            measurements
            + { leftHand: measurements.leftHand + { upper: primaryStep.events[0].value; }; }
            + { rightHand: measurements.rightHand + { upper: primaryStep.events[0].value; }; }
          else measurements + { rightHand: measurements.rightHand + { upper: primaryStep.events[0].value; }; };
        m2:
          if !secondaryChanged then m1
          else if sameSides then
            m1
            + { leftHand: m1.leftHand + { lower: secondaryStep.events[0].value; }; }
            + { rightHand: m1.rightHand + { lower: secondaryStep.events[0].value; }; }
          else m1 + { rightHand: m1.rightHand + { lower: secondaryStep.events[0].value; }; };
        eval m2;
      }
      else if selectedPart == "leftLeg" then
      {
        m1:
          if !primaryChanged then measurements
          else if sameSides then
            measurements
            + { leftLeg: measurements.leftLeg + { upper: primaryStep.events[0].value; }; }
            + { rightLeg: measurements.rightLeg + { upper: primaryStep.events[0].value; }; }
          else measurements + { leftLeg: measurements.leftLeg + { upper: primaryStep.events[0].value; }; };
        m2:
          if !secondaryChanged then m1
          else if sameSides then
            m1
            + { leftLeg: m1.leftLeg + { lower: secondaryStep.events[0].value; }; }
            + { rightLeg: m1.rightLeg + { lower: secondaryStep.events[0].value; }; }
          else m1 + { leftLeg: m1.leftLeg + { lower: secondaryStep.events[0].value; }; };
        eval m2;
      }
      else if selectedPart == "rightLeg" then
      {
        m1:
          if !primaryChanged then measurements
          else if sameSides then
            measurements
            + { leftLeg: measurements.leftLeg + { upper: primaryStep.events[0].value; }; }
            + { rightLeg: measurements.rightLeg + { upper: primaryStep.events[0].value; }; }
          else measurements + { rightLeg: measurements.rightLeg + { upper: primaryStep.events[0].value; }; };
        m2:
          if !secondaryChanged then m1
          else if sameSides then
            m1
            + { leftLeg: m1.leftLeg + { lower: secondaryStep.events[0].value; }; }
            + { rightLeg: m1.rightLeg + { lower: secondaryStep.events[0].value; }; }
          else m1 + { rightLeg: m1.rightLeg + { lower: secondaryStep.events[0].value; }; };
        eval m2;
      }
      else error("expected selectedPart step");

    nextMeasurementsWithBend:
      if !bendChanged then nextMeasurements
      else
      {
        flip: bendStep.events[0].value;
        flipFactor: if flip then -1 else 1;
        leftSign: -1 * flipFactor;
        rightSign: 1 * flipFactor;
        eval
          if selectedPart == "leftArm" or selectedPart == "rightArm" then
            if nextSameSides then
              nextMeasurements
              + { leftHand: nextMeasurements.leftHand + { sign: leftSign; }; }
              + { rightHand: nextMeasurements.rightHand + { sign: rightSign; }; }
            else if selectedPart == "leftArm" then
              nextMeasurements + { leftHand: nextMeasurements.leftHand + { sign: leftSign; }; }
            else nextMeasurements + { rightHand: nextMeasurements.rightHand + { sign: rightSign; }; }
          else if selectedPart == "leftLeg" or selectedPart == "rightLeg" then
            if nextSameSides then
              nextMeasurements
              + { leftLeg: nextMeasurements.leftLeg + { sign: leftSign; }; }
              + { rightLeg: nextMeasurements.rightLeg + { sign: rightSign; }; }
            else if selectedPart == "leftLeg" then
              nextMeasurements + { leftLeg: nextMeasurements.leftLeg + { sign: leftSign; }; }
            else nextMeasurements + { rightLeg: nextMeasurements.rightLeg + { sign: rightSign; }; }
          else error("expected limb selectedPart");
      };

    handledByUi: primaryStep != null or secondaryStep != null or toggleStep != null or bendStep != null;

    handleDown:
      if handledByUi or dragging != null then null
      else if event.type == "pointer" and event.action == "down" then pickHandle(event.point)
      else null;

    nextDragging:
      if dragging == null then
        if handleDown == null then null
        else
        {
          p: [event.point.x, event.point.y];
          center:
            if handleDown == "anchor" then anchor
            else if handleDown == "leftHandEnd" then geometry.leftHand.to
            else if handleDown == "rightHandEnd" then geometry.rightHand.to
            else if handleDown == "leftLegEnd" then geometry.leftLeg.to
            else if handleDown == "rightLegEnd" then geometry.rightLeg.to
            else error("expected handleDown");
          eval
          {
            kind: handleDown;
            pointerId: event.pointer.id;
            offset: [center[0] - p[0], center[1] - p[1]];
          };
        }
      else if event.type == "pointer" and (event.action == "up" or event.action == "cancel") and event.pointer.id == dragging.pointerId then null
      else dragging;

    dragPoint:
      if nextDragging == null then null
      else if event.type == "pointer" and (event.action == "move" or event.action == "down") and event.pointer.id == nextDragging.pointerId then
      {
        p: [event.point.x, event.point.y];
        eval [p[0] + nextDragging.offset[0], p[1] + nextDragging.offset[1]];
      }
      else null;

    nextAnchor:
      if dragPoint == null then anchor
      else if nextDragging.kind == "anchor" then dragPoint
      else anchor;

    nextMeasurementsWithDrag:
      if dragPoint == null then nextMeasurementsWithBend
      else if nextDragging.kind == "leftHandEnd" then
      {
        end: [dragPoint[0] - geometry.leftHand.from[0], dragPoint[1] - geometry.leftHand.from[1]];
        eval nextMeasurementsWithBend + { leftHand: nextMeasurementsWithBend.leftHand + { end; }; };
      }
      else if nextDragging.kind == "rightHandEnd" then
      {
        end: [dragPoint[0] - geometry.rightHand.from[0], dragPoint[1] - geometry.rightHand.from[1]];
        eval nextMeasurementsWithBend + { rightHand: nextMeasurementsWithBend.rightHand + { end; }; };
      }
      else if nextDragging.kind == "leftLegEnd" then
      {
        end: [dragPoint[0] - geometry.leftLeg.from[0], dragPoint[1] - geometry.leftLeg.from[1]];
        eval nextMeasurementsWithBend + { leftLeg: nextMeasurementsWithBend.leftLeg + { end; }; };
      }
      else if nextDragging.kind == "rightLegEnd" then
      {
        end: [dragPoint[0] - geometry.rightLeg.from[0], dragPoint[1] - geometry.rightLeg.from[1]];
        eval nextMeasurementsWithBend + { rightLeg: nextMeasurementsWithBend.rightLeg + { end; }; };
      }
      else nextMeasurementsWithBend;

    dragSelectedPart:
      if handleDown == "leftHandEnd" then "leftArm"
      else if handleDown == "rightHandEnd" then "rightArm"
      else if handleDown == "leftLegEnd" then "leftLeg"
      else if handleDown == "rightLegEnd" then "rightLeg"
      else null;

    hoverCandidate:
      if event.type == "pointer" and (event.action == "move" or event.action == "down") then pickPart(event.point)
      else hoveredPart;

    hoverChanged: hoverCandidate != hoveredPart;

    clickedPart:
      if handledByUi or handleDown != null then null
      else if event.type == "pointer" and event.action == "down" then pickPart(event.point)
      else null;

    nextSelectedPart:
      if dragSelectedPart != null then dragSelectedPart
      else if clickedPart == null then selectedPart
      else clickedPart;

    selectionChanged: nextSelectedPart != selectedPart;

    nextPrimarySliderState:
      if selectionChanged then null
      else if primaryStep == null then primarySliderState
      else primaryStep.state;

    nextSecondarySliderState:
      if !hasSecondary then null
      else if selectionChanged then null
      else if secondaryStep == null then secondarySliderState
      else secondaryStep.state;

    nextSameSidesToggleState: if toggleStep == null then sameSidesToggleState else toggleStep.state;

    nextBendToggleState:
      if !hasSecondary then null
      else if selectionChanged then null
      else if bendStep == null then bendToggleState
      else bendStep.state;

    dragChanged: nextDragging != dragging;
    anchorChanged: nextAnchor != anchor;
    measurementsChanged: nextMeasurementsWithDrag != measurements;

    eval
    if primaryStep == null and secondaryStep == null and toggleStep == null and bendStep == null and !selectionChanged and !hoverChanged and !dragChanged and !anchorChanged and !measurementsChanged then null else
    {
      state:
      {
        anchor: nextAnchor;
        measurements: nextMeasurementsWithDrag;
        selectedPart: nextSelectedPart;
        hoveredPart: hoverCandidate;
        dragging: nextDragging;
        primarySlider: nextPrimarySliderState;
        secondarySlider: nextSecondarySliderState;
        sameSidesToggle: nextSameSidesToggleState;
        bendToggle: nextBendToggleState;
        sameSides: nextSameSides;
      };
      events: [];
    };
  };

  eval
  {
    view;
    graphics:
      [
        characterGraphics,
        hoverHighlight,
        highlight,
        handles,
        panelBackground,
        panelTitle,
        panelHint,
        panelSelected,
        sameSidesToggle.graphics,
        if bendToggle == null then [] else bendToggle.graphics,
        primarySlider.graphics,
        if secondarySlider == null then [] else secondarySlider.graphics
      ];
    step: stepper;
  };
}
