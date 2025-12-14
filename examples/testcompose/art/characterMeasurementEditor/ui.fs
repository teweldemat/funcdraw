{
  build: (args) =>
  {
    slider: args.slider;
    toggle: args.toggle;
    view: args.view;
    selectedPart: args.selectedPart;
    sameSides: args.sameSides;
    selection: args.selection;

    primarySliderState: args.primarySliderState;
    secondarySliderState: args.secondarySliderState;
    sameSidesToggleState: args.sameSidesToggleState;
    bendToggleState: args.bendToggleState;

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

    primarySlider:
      slider(
        {
          position: [uiX, sliderY1];
          size: [uiWidth, sliderHeight];
          min: selection.primaryRange.min;
          max: selection.primaryRange.max;
          value: selection.primaryValue;
          label: selection.primaryLabel;
        },
        primarySliderState);

    secondarySlider:
      if !selection.hasSecondary then null else
        slider(
          {
            position: [uiX, sliderY2];
            size: [uiWidth, sliderHeight];
            min: selection.secondaryRange.min;
            max: selection.secondaryRange.max;
            value: selection.secondaryValue;
            label: selection.secondaryLabel;
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

    bendToggle:
      if !selection.hasSecondary then null else
        toggle(
          {
            position: [uiX, bendToggleY];
            size: [uiWidth, toggleHeight];
            value: selection.bendFlipped;
            label: "flip bend";
          },
          bendToggleState);

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

    eval
    {
      panel;
      panelBackground;
      panelTitle;
      panelHint;
      panelSelected;
      sameSidesToggle;
      bendToggle;
      primarySlider;
      secondarySlider;
    };
  };
}
