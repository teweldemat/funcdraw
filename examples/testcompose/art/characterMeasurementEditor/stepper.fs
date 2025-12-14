{
  make: (args) =>
  {
    anchor: args.anchor;
    measurements: args.measurements;
    selectedPart: args.selectedPart;
    hoveredPart: args.hoveredPart;
    dragging: args.dragging;

    primarySliderState: args.primarySliderState;
    secondarySliderState: args.secondarySliderState;
    sameSidesToggleState: args.sameSidesToggleState;
    bendToggleState: args.bendToggleState;
    sameSides: args.sameSides;

    geometry: args.geometry;
    pickPart: args.pickPart;
    pickHandle: args.pickHandle;

    primarySlider: args.primarySlider;
    secondarySlider: args.secondarySlider;
    sameSidesToggle: args.sameSidesToggle;
    bendToggle: args.bendToggle;

    hasSecondary:
      selectedPart == "leftArm" or
      selectedPart == "rightArm" or
      selectedPart == "leftLeg" or
      selectedPart == "rightLeg";

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

    eval stepper;
  };
}
