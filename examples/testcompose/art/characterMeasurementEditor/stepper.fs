{
  make: (args) =>
  {
    anchor: args.anchor;
    anchorDotState: args.anchorDotState;
    leftHandEndDotState: args.leftHandEndDotState;
    rightHandEndDotState: args.rightHandEndDotState;
    leftLegEndDotState: args.leftLegEndDotState;
    rightLegEndDotState: args.rightLegEndDotState;
    measurements: args.measurements;
    selectedPart: args.selectedPart;
    hoveredPart: args.hoveredPart;

    primarySliderState: args.primarySliderState;
    secondarySliderState: args.secondarySliderState;
    sameSidesToggleState: args.sameSidesToggleState;
    bendToggleState: args.bendToggleState;
    sameSides: args.sameSides;

    geometry: args.geometry;
    pickPart: args.pickPart;

    primarySlider: args.primarySlider;
    secondarySlider: args.secondarySlider;
    sameSidesToggle: args.sameSidesToggle;
    bendToggle: args.bendToggle;

    anchorDot: args.anchorDot;
    leftHandEndDot: args.leftHandEndDot;
    rightHandEndDot: args.rightHandEndDot;
    leftLegEndDot: args.leftLegEndDot;
    rightLegEndDot: args.rightLegEndDot;

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

      isDragging: (s) => s != null and s.pointerId != null;

      anchorDotStep: if handledByUi and !isDragging(anchorDotState) then null else anchorDot.step(event);
      leftHandEndStep: if handledByUi and !isDragging(leftHandEndDotState) then null else leftHandEndDot.step(event);
      rightHandEndStep: if handledByUi and !isDragging(rightHandEndDotState) then null else rightHandEndDot.step(event);
      leftLegEndStep: if handledByUi and !isDragging(leftLegEndDotState) then null else leftLegEndDot.step(event);
      rightLegEndStep: if handledByUi and !isDragging(rightLegEndDotState) then null else rightLegEndDot.step(event);

      dragStart: (s) => s != null and Len(s.events) > 0 and s.events[0].action == "dragstart";
      dotChanged: (s) => s != null and Len(s.events) > 0 and s.events[0].action == "change";
      dotValue: (s) => if dotChanged(s) then s.events[0].value else null;

      handleDown:
        if event.type != "pointer" or event.action != "down" then null
        else if dragStart(anchorDotStep) then "anchor"
        else if dragStart(leftHandEndStep) then "leftHandEnd"
        else if dragStart(rightHandEndStep) then "rightHandEnd"
        else if dragStart(leftLegEndStep) then "leftLegEnd"
        else if dragStart(rightLegEndStep) then "rightLegEnd"
        else null;

      nextAnchor: dotValue(anchorDotStep)??anchor;
      nextAnchorDotState: if anchorDotStep == null then anchorDotState else anchorDotStep.state;
      nextLeftHandEndDotState: if leftHandEndStep == null then leftHandEndDotState else leftHandEndStep.state;
      nextRightHandEndDotState: if rightHandEndStep == null then rightHandEndDotState else rightHandEndStep.state;
      nextLeftLegEndDotState: if leftLegEndStep == null then leftLegEndDotState else leftLegEndStep.state;
      nextRightLegEndDotState: if rightLegEndStep == null then rightLegEndDotState else rightLegEndStep.state;

      nextMeasurementsWithDrag:
      {
        m1:
          if !dotChanged(leftHandEndStep) then nextMeasurementsWithBend
          else
          {
            p: leftHandEndStep.events[0].value;
            end: [p[0] - geometry.leftHand.from[0], p[1] - geometry.leftHand.from[1]];
            eval nextMeasurementsWithBend + { leftHand: nextMeasurementsWithBend.leftHand + { end; }; };
          };
        m2:
          if !dotChanged(rightHandEndStep) then m1
          else
          {
            p: rightHandEndStep.events[0].value;
            end: [p[0] - geometry.rightHand.from[0], p[1] - geometry.rightHand.from[1]];
            eval m1 + { rightHand: m1.rightHand + { end; }; };
          };
        m3:
          if !dotChanged(leftLegEndStep) then m2
          else
          {
            p: leftLegEndStep.events[0].value;
            end: [p[0] - geometry.leftLeg.from[0], p[1] - geometry.leftLeg.from[1]];
            eval m2 + { leftLeg: m2.leftLeg + { end; }; };
          };
        m4:
          if !dotChanged(rightLegEndStep) then m3
          else
          {
            p: rightLegEndStep.events[0].value;
            end: [p[0] - geometry.rightLeg.from[0], p[1] - geometry.rightLeg.from[1]];
            eval m3 + { rightLeg: m3.rightLeg + { end; }; };
          };
        eval m4;
      };

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

      measurementsChanged: nextMeasurementsWithDrag != measurements;
      dotsChanged:
        anchorDotStep != null or
        leftHandEndStep != null or
        rightHandEndStep != null or
        leftLegEndStep != null or
        rightLegEndStep != null;

      eval
      if primaryStep == null and secondaryStep == null and toggleStep == null and bendStep == null and !selectionChanged and !hoverChanged and !dotsChanged and !measurementsChanged then null else
      {
        state:
        {
          anchor: nextAnchor;
          anchorDot: nextAnchorDotState;
          leftHandEndDot: nextLeftHandEndDotState;
          rightHandEndDot: nextRightHandEndDotState;
          leftLegEndDot: nextLeftLegEndDotState;
          rightLegEndDot: nextRightLegEndDotState;
          measurements: nextMeasurementsWithDrag;
          selectedPart: nextSelectedPart;
          hoveredPart: hoverCandidate;
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
