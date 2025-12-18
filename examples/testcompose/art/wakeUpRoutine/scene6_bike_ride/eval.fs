(localT) =>
{
  s: scene6Segments;

  stage:
    if localT < s.arriveDuration then "scene6a_bus_arrive"
    else if localT < s.arriveDuration + s.doorOpenDuration then "scene6b_door_open"
    else if localT < s.arriveDuration + s.doorOpenDuration + s.exitDuration then "scene6c_exit_bus"
    else if localT < s.arriveDuration + s.doorOpenDuration + s.exitDuration + s.departDuration then "scene6d_bus_drives_away"
    else if localT < s.arriveDuration + s.doorOpenDuration + s.exitDuration + s.departDuration + s.walkToBikeDuration then "scene6e_walk_to_bike"
    else if localT < s.arriveDuration + s.doorOpenDuration + s.exitDuration + s.departDuration + s.walkToBikeDuration + s.mountDuration then "scene6f_mount_bike"
    else "scene6g_ride_bike";

  stageT:
    if stage == "scene6a_bus_arrive" then localT
    else if stage == "scene6b_door_open" then localT - s.doorOffset
    else if stage == "scene6c_exit_bus" then localT - s.exitOffset
    else if stage == "scene6d_bus_drives_away" then localT - s.departOffset
    else if stage == "scene6e_walk_to_bike" then localT - s.walkOffset
    else if stage == "scene6f_mount_bike" then localT - s.mountOffset
    else localT - s.rideOffset;

  sceneFn:
    if stage == "scene6a_bus_arrive" then scene6a_bus_arrive
    else if stage == "scene6b_door_open" then scene6b_door_open
    else if stage == "scene6c_exit_bus" then scene6c_exit_bus
    else if stage == "scene6d_bus_drives_away" then scene6d_bus_drives_away
    else if stage == "scene6e_walk_to_bike" then scene6e_walk_to_bike
    else if stage == "scene6f_mount_bike" then scene6f_mount_bike
    else scene6g_ride_bike;

  scene: sceneFn(stageT);

  eval scene;
}
