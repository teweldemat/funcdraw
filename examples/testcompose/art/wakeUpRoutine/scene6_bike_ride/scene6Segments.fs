{
  arriveDuration: 4;
  doorOpenDuration: 1.2;
  exitDuration: 2.2;
  departDuration: 3;
  walkToBikeDuration: 3.2;
  mountDuration: 0.6;
  rideDuration: 5.8;

  doorOffset: arriveDuration;
  exitOffset: arriveDuration + doorOpenDuration;
  departOffset: arriveDuration + doorOpenDuration + exitDuration;
  walkOffset: arriveDuration + doorOpenDuration + exitDuration + departDuration;
  mountOffset: arriveDuration + doorOpenDuration + exitDuration + departDuration + walkToBikeDuration;
  rideOffset: arriveDuration + doorOpenDuration + exitDuration + departDuration + walkToBikeDuration + mountDuration;

  totalDuration:
    arriveDuration
    + doorOpenDuration
    + exitDuration
    + departDuration
    + walkToBikeDuration
    + mountDuration
    + rideDuration;
}
