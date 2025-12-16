{
  s1: common.scene1Duration;
  s2: common.scene2Duration;
  s3: common.scene3Duration;
  s4: common.scene4Duration;
  s5: common.scene5Duration;

  stage:
    if t < s1 then "scene1"
    else if t < s1 + s2 then "scene2"
    else if t < s1 + s2 + s3 then "scene3"
    else if t < s1 + s2 + s3 + s4 then "scene4"
    else if t < s1 + s2 + s3 + s4 + s5 then "scene5"
    else "end";

  stageT:
    if stage == "scene1" then t
    else if stage == "scene2" then t - s1
    else if stage == "scene3" then t - s1 - s2
    else if stage == "scene4" then t - s1 - s2 - s3
    else if stage == "scene5" then t - s1 - s2 - s3 - s4
    else s5;

  sceneFn:
    if stage == "scene1" then scene1
    else if stage == "scene2" then scene2
    else if stage == "scene3" then scene3
    else if stage == "scene4" then scene4
    else scene5;

  scene: sceneFn(stageT);

  label:
  {
    type: "text";
    text: join(["wakeUpRoutine · ", stage], "");
    position: [scene.view.left + 4, scene.view.top - 6];
    fontSize: 2.4;
    color: "#0f172a";
  };

  eval scene + { graphics: scene.graphics + [label]; common; actor; backdrop; scene1; scene2; scene3; scene4; scene5; };
}
