{
  bicycle: package("@funcdraw/testlib").cartoon.bicycle;

  wheelAngle: t * 3.0;
  rearCenter: [-20, 0];
  bike: bicycle(rearCenter, 9, wheelAngle, "#9ca3af", "#6b7280");

  viewHeight: 70;
  ratio: canvas.size.width / canvas.size.height;
  viewWidth: viewHeight * ratio;
  centerX: rearCenter[0] + bike.wheelBase / 2;
  left: centerX - viewWidth / 2;
  bottom: -25;
  view: { left; bottom; right: left + viewWidth; top: bottom + viewHeight; };

  eval { view; graphics: bike.graphics; };
}

