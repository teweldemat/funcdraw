{
  stage:
    if t < common.scene1Duration then "scene1"
    else if t < common.scene1Duration + common.scene2Duration then "scene2"
    else if t < common.cycleDuration then "scene3"
    else "end";

  stageT:
    if stage == "scene1" then t
    else if stage == "scene2" then t - common.scene1Duration
    else if stage == "scene3" then t - common.scene1Duration - common.scene2Duration
    else common.scene3Duration;

  sceneFn:
    if stage == "scene1" then scene1
    else if stage == "scene2" then scene2
    else scene3;

  scene: sceneFn(stageT);

  label:
  {
    type: "text";
    text: join(["wakeUpRoutine · ", stage], "");
    position: [scene.view.left + 4, scene.view.top - 6];
    fontSize: 2.4;
    color: "#0f172a";
  };

  eval scene + { graphics: scene.graphics + [label]; common; actor; backdrop; scene1; scene2; scene3; };
}
