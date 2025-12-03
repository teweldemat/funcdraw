const consts = typeof constants === 'object' && constants ? constants : {};
const scene1Duration = Number.isFinite(consts.scene1Duration)
  ? consts.scene1Duration
  : 4.5; // door + zoom from scene1.js
const scene2Duration = Number.isFinite(consts.scene2?.duration)
  ? consts.scene2.duration
  : 3; // requested 3s walk
const scene3DoorDuration = Number.isFinite(consts.scene3?.doorDuration)
  ? consts.scene3.doorDuration
  : 1.5;
const scene3ZoomDuration = Number.isFinite(consts.scene3?.zoomDuration)
  ? consts.scene3.zoomDuration
  : 3;
const scene3Duration = Number.isFinite(consts.scene3?.duration)
  ? consts.scene3.duration
  : scene3DoorDuration + scene3ZoomDuration; // reverse of scene1 timing
const totalDuration = scene1Duration + scene2Duration + scene3Duration;

const timeValue = typeof t === 'number' ? t : 0;
const clampedTime = Math.min(Math.max(timeValue, 0), totalDuration);

if (clampedTime < scene1Duration) {
  return scene1(clampedTime);
}

// Hand the final pose of scene1 into scene2 so it can keep walking.
const scene1EndTime = Math.max(scene1Duration - 1e-3, 0); // avoid wrapping scene1's modulo loop
const scene1EndPose = scene1(scene1EndTime);
const scene2Time = Math.min(clampedTime - scene1Duration, scene2Duration);
const scene2EndTime = Math.max(scene2Duration - 1e-3, 0);
const scene2EndPose = scene2(scene2EndTime, scene1EndPose);

if (clampedTime < scene1Duration + scene2Duration) {
  return scene2(scene2Time, scene1EndPose);
}

const scene3Time = Math.min(clampedTime - scene1Duration - scene2Duration, scene3Duration);
return scene3(scene3Time, scene2EndPose);
