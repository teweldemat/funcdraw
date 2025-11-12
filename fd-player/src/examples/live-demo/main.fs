{
  fallbackSceneResult: { title: 'Scene unavailable'; graphics: []; duration: 0 };
  fallbackScene: (timeParam) => fallbackSceneResult;

  scene1GeneratorRaw: scene.scene1 ?? fallbackScene;
  scene2GeneratorRaw: scene.scene2 ?? fallbackScene;
  scene3GeneratorRaw: scene.scene3 ?? fallbackScene;
  scene4GeneratorRaw: scene.scene4 ?? fallbackScene;
  scene5GeneratorRaw: scene.scene5 ?? fallbackScene;
  scene1Generator:
    if (scene1GeneratorRaw)
      then scene1GeneratorRaw
      else fallbackScene;
  scene2Generator:
    if (scene2GeneratorRaw)
      then scene2GeneratorRaw
      else fallbackScene;
  scene3Generator:
    if (scene3GeneratorRaw)
      then scene3GeneratorRaw
      else fallbackScene;
  scene4Generator:
    if (scene4GeneratorRaw)
      then scene4GeneratorRaw
      else fallbackScene;
  scene5Generator:
    if (scene5GeneratorRaw)
      then scene5GeneratorRaw
      else fallbackScene;

  globalTime: t ?? 0;
  defaultSceneDuration: 15;
  sceneTimeNormalized: if (globalTime < 0) then 0 else globalTime;

  scene1Result: scene1Generator(sceneTimeNormalized);
  scene1DurationRaw: scene1Result.duration ?? defaultSceneDuration;
  scene1Duration:
    if (scene1DurationRaw <= 0)
      then defaultSceneDuration
      else scene1DurationRaw;

  scene2TimeRaw: sceneTimeNormalized - scene1Duration;
  scene2LocalTime: if (scene2TimeRaw < 0) then 0 else scene2TimeRaw;
  scene2Result: scene2Generator(scene2LocalTime);
  scene2DurationRaw: scene2Result.duration ?? defaultSceneDuration;
  scene2Duration:
    if (scene2DurationRaw <= 0)
      then defaultSceneDuration
      else scene2DurationRaw;

  scene3TimeRaw: sceneTimeNormalized - scene1Duration - scene2Duration;
  scene3LocalTime: if (scene3TimeRaw < 0) then 0 else scene3TimeRaw;
  scene3Result: scene3Generator(scene3LocalTime);
  scene3DurationRaw: scene3Result.duration ?? defaultSceneDuration;
  scene3Duration:
    if (scene3DurationRaw <= 0)
      then defaultSceneDuration
      else scene3DurationRaw;

  scene4TimeRaw: sceneTimeNormalized - scene1Duration - scene2Duration - scene3Duration;
  scene4LocalTime: if (scene4TimeRaw < 0) then 0 else scene4TimeRaw;
  scene4Result: scene4Generator(scene4LocalTime);
  scene4DurationRaw: scene4Result.duration ?? defaultSceneDuration;
  scene4Duration:
    if (scene4DurationRaw <= 0)
      then defaultSceneDuration
      else scene4DurationRaw;

  scene5TimeRaw: sceneTimeNormalized - scene1Duration - scene2Duration - scene3Duration - scene4Duration;
  scene5LocalTime: if (scene5TimeRaw < 0) then 0 else scene5TimeRaw;
  scene5Result: scene5Generator(scene5LocalTime);

  activeScene:
    if (sceneTimeNormalized < scene1Duration)
      then scene1Result
      else if (sceneTimeNormalized < scene1Duration + scene2Duration)
        then scene2Result
      else if (sceneTimeNormalized < scene1Duration + scene2Duration + scene3Duration)
        then scene3Result
        else if (sceneTimeNormalized < scene1Duration + scene2Duration + scene3Duration + scene4Duration)
          then scene4Result
          else scene5Result;
  sceneGraphics: activeScene.graphics ?? [];

  eval sceneGraphics;
}
