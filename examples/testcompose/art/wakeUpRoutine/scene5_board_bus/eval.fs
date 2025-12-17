(localT) =>
{
  s5a: common.scene5aDuration;

  stage:
    if localT < s5a then "scene5a_board_bus"
    else "scene5b_bus_leaves";

  stageT:
    if stage == "scene5a_board_bus" then localT
    else localT - s5a;

  sceneFn:
    if stage == "scene5a_board_bus" then scene5a_board_bus
    else scene5b_bus_leaves;

  scene: sceneFn(stageT);

  eval scene;
}
