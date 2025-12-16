(view, sunProgress, globalT) =>
{
  landscape: package("@funcdraw/testlib").cartoon.landscape;

  loop: (value, period) => value - math.Floor(value / period) * period;
  lerp: (a, b, p) => a + (b - a) * p;
  darknessAlpha: 0.95 * (1 - sunProgress);
  darknessFill: common.alphaHex("#020617", darknessAlpha);

  starField: (sunProgress) =>
  {
    alpha: (1 - sunProgress) * 0.85;
    fill: common.alphaHex("#e2e8f0", alpha);
    w: view.right - view.left;
    h: view.top - view.bottom;
    count: 40;
    eval
      Range(0, count) map (k, idx) =>
      {
        x: view.left + loop(k * 37, w);
        y: view.bottom + loop(k * 53, h);
        r: 0.18 + loop(k * 0.07, 0.35);
        eval { type: "circle"; name: "star"; center: [x, y]; radius: r; fill; stroke: "none"; width: 0; };
      };
  };

  skyNight:
  {
    type: "rect";
    name: "sky-night";
    position: [view.left, view.bottom];
    size: [view.right - view.left, view.top - view.bottom];
    fill: "#020617";
    stroke: "none";
    width: 0;
  };

  skyDay:
  {
    type: "rect";
    name: "sky-day";
    position: [view.left, view.bottom];
    size: [view.right - view.left, view.top - view.bottom];
    fill: common.alphaHex("#93c5fd", sunProgress);
    stroke: "none";
    width: 0;
  };

  sunX: 70;
  sunY: lerp(-18, 65, sunProgress);
  sunR: 6 + 2 * sunProgress;
  sun:
    landscape.sun(
      {
        center: [sunX, sunY];
        radius: sunR;
        fill: "#fde047";
        stroke: "#f59e0b";
        width: 0.25;
        rayCount: 16;
        rayLength: 4 + 2 * sunProgress;
        rayColor: "#fde047";
        rayWidth: 0.18;
        rotation: globalT * 0.12;
      });

  driftSpan: (view.right - view.left) + 220;
  cloudFill: "#f1f5f9";
  cloudStroke: "#cbd5e1";
  cloudStrokeWidth: 0.18;
  cloud1X: view.left - 80 + loop(globalT * 3.2, driftSpan);
  cloud2X: view.left - 140 + loop(globalT * 2.1, driftSpan);
  cloud3X: view.left - 40 + loop(globalT * 1.6, driftSpan);

  clouds:
  [
    landscape.cloud({ center: [cloud1X, 68]; width: 46; height: 18; fill: cloudFill; stroke: cloudStroke; strokeWidth: cloudStrokeWidth; }),
    landscape.cloud({ center: [cloud2X, 54]; width: 58; height: 22; fill: cloudFill; stroke: cloudStroke; strokeWidth: cloudStrokeWidth; }),
    landscape.cloud({ center: [cloud3X, 60]; width: 38; height: 15; fill: cloudFill; stroke: cloudStroke; strokeWidth: cloudStrokeWidth; })
  ];

  farHill:
  {
    type: "polygon";
    name: "hill";
    points:
    [
      [view.left, -12],
      [view.left + 40, 5],
      [view.left + 85, -6],
      [view.left + 130, 8],
      [view.left + 175, -4],
      [view.right, 2],
      [view.right, -25],
      [view.left, -25]
    ];
    fill: "#166534";
    stroke: "none";
    width: 0;
  };

  hillShade:
  {
    type: "polygon";
    name: "hill-shade";
    points: farHill.points;
    fill: darknessFill;
    stroke: "none";
    width: 0;
  };

  yard:
  {
    type: "rect";
    name: "yard";
    position: [view.left, common.sidewalkTopY];
    size: [view.right - view.left, (common.yardTopY + 18) - common.sidewalkTopY];
    fill: "#a16207";
    stroke: "none";
    width: 0;
  };

  yardShade:
  {
    type: "rect";
    name: "yard-shade";
    position: yard.position;
    size: yard.size;
    fill: darknessFill;
    stroke: "none";
    width: 0;
  };

  sidewalk:
  {
    type: "rect";
    name: "sidewalk";
    position: [view.left, common.roadTopY];
    size: [view.right - view.left, common.sidewalkTopY - common.roadTopY];
    fill: "#94a3b8";
    stroke: "none";
    width: 0;
  };

  sidewalkShade:
  {
    type: "rect";
    name: "sidewalk-shade";
    position: sidewalk.position;
    size: sidewalk.size;
    fill: darknessFill;
    stroke: "none";
    width: 0;
  };

  road:
  {
    type: "rect";
    name: "road";
    position: [view.left, common.roadBottomY];
    size: [view.right - view.left, common.roadTopY - common.roadBottomY];
    fill: "#334155";
    stroke: "none";
    width: 0;
  };

  laneDashCount: 16;
  laneDashW: 10;
  laneDashH: 1.2;
  laneGap: 4;
  laneStartX: view.left + 10;
  laneY: (common.roadTopY + common.roadBottomY) / 2 - laneDashH / 2;
  laneDashes:
    Range(0, laneDashCount) map (k, idx) =>
    {
      x: laneStartX + k * (laneDashW + laneGap);
      eval { type: "rect"; name: "lane"; position: [x, laneY]; size: [laneDashW, laneDashH]; fill: "#e2e8f0"; stroke: "none"; width: 0; };
    };

  roadShade:
  {
    type: "rect";
    name: "road-shade";
    position: road.position;
    size: road.size;
    fill: darknessFill;
    stroke: "none";
    width: 0;
  };

  eval [skyNight] + starField(sunProgress) + [skyDay, sun, clouds, farHill, hillShade, yard, yardShade, sidewalk, sidewalkShade, road, laneDashes, roadShade];
}
