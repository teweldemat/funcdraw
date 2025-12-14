{
  build: (args) =>
  {
    geometry: args.geometry;
    headCenter: args.headCenter;
    selectedPart: args.selectedPart;
    hoveredPart: args.hoveredPart;
    anchor: args.anchor;
    dragging: args.dragging;
    handleRadius: args.handleRadius;

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

    eval { highlight; hoverHighlight; handles; };
  };
}
