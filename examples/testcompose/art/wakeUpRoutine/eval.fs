{
  s1: common.scene1Duration;
  s2: common.scene2Duration;
  s3: common.scene3Duration;
  s4: common.scene4Duration;
  s5: common.scene5Duration;

  stage:
    if t < s1 then "scene1_dawn"
    else if t < s1 + s2 then "scene2_exit_house"
    else if t < s1 + s2 + s3 then "scene3_walk_to_stop"
    else if t < s1 + s2 + s3 + s4 then "scene4_bus_arrival"
    else if t < s1 + s2 + s3 + s4 + s5 then "scene5_board_bus"
    else "end";

  stageT:
    if stage == "scene1_dawn" then t
    else if stage == "scene2_exit_house" then t - s1
    else if stage == "scene3_walk_to_stop" then t - s1 - s2
    else if stage == "scene4_bus_arrival" then t - s1 - s2 - s3
    else if stage == "scene5_board_bus" then t - s1 - s2 - s3 - s4
    else s5;

  sceneFn:
    if stage == "scene1_dawn" then scene1_dawn
    else if stage == "scene2_exit_house" then scene2_exit_house
    else if stage == "scene3_walk_to_stop" then scene3_walk_to_stop
    else if stage == "scene4_bus_arrival" then scene4_bus_arrival
    else scene5_board_bus;

  scene: sceneFn(stageT);

  label:
  {
    type: "text";
    text: join(["wakeUpRoutine · ", stage], "");
    position: [scene.view.left + 4, scene.view.top - 6];
    fontSize: 2.4;
    color: "#0f172a";
  };

  eval scene + { graphics: scene.graphics + [label]; common; actor; backdrop; scene1_dawn; scene2_exit_house; scene3_walk_to_stop; scene4_bus_arrival; scene5_board_bus; };
}
