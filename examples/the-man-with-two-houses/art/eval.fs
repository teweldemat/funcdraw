{
  consts:constants ?? {};

  scene1Duration:consts.scene1Duration ?? 4.5;
  scene2Duration:consts.scene2.duration ?? 3;
  scene3DoorDuration:consts.scene3.doorDuration ?? 1.5;
  scene3ZoomDuration:consts.scene3.zoomDuration ?? 3;
  scene3Duration:consts.scene3.duration ?? (scene3DoorDuration + scene3ZoomDuration);
  totalDuration:scene1Duration + scene2Duration + scene3Duration;

  timeValue:t ?? 0;
  clampedTime:math.max(0, math.min(timeValue, totalDuration));

  scene1EndTime:math.max(scene1Duration - 0.001, 0);
  scene1EndPose:scene1(scene1EndTime);

  scene2Time:math.min(math.max(clampedTime - scene1Duration, 0), scene2Duration);
  scene2EndTime:math.max(scene2Duration - 0.001, 0);
  scene2EndPose:scene2(scene2EndTime, scene1EndPose);

  scene3Time:math.min(math.max(clampedTime - scene1Duration - scene2Duration, 0), scene3Duration);

  selected:if clampedTime < scene1Duration then
    scene1(clampedTime)
  else if clampedTime < scene1Duration + scene2Duration then
    scene2(scene2Time, scene1EndPose)
  else
    scene3(scene3Time, scene2EndPose);

  view:selected.view;
  graphics:selected.graphics;
  manPosition:selected.manPosition;
  manMeasurements:selected.manMeasurements;
}
