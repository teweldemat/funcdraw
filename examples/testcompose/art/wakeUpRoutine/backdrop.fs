(view, sunProgress, globalT) =>
{
  landscape: package("@funcdraw/testlib").cartoon.landscape;

  loop: (value, period) => value - math.Floor(value / period) * period;
  lerp: (a, b, p) => a + (b - a) * p;
  darknessAlpha: 0.95 * (1 - sunProgress);
  darknessFill: fd.color.alpha("#020617", darknessAlpha);

  starField: (sunProgress) =>
  {
    alpha: (1 - sunProgress) * 0.85;
    fill: fd.color.alpha("#e2e8f0", alpha);
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
    fill: fd.color.alpha("#93c5fd", sunProgress);
    stroke: "none";
    width: 0;
  };

  sunX: view.left + (view.right - view.left) * 0.82;
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

  sidewalkH: common.sidewalkTopY - common.roadTopY;
  farSidewalk:
  {
    type: "rect";
    name: "sidewalk-far";
    position: [view.left, common.roadBottomY - sidewalkH];
    size: [view.right - view.left, sidewalkH];
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

  farSidewalkShade:
  {
    type: "rect";
    name: "sidewalk-far-shade";
    position: farSidewalk.position;
    size: farSidewalk.size;
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

  laneDashW: 10;
  laneDashH: 1.2;
  laneGap: 4;
  lanePeriod: laneDashW + laneGap;
  laneOriginX: 0;
  laneStartIndex: math.Floor((view.left - laneOriginX) / lanePeriod) - 1;
  laneDashCount: math.Ceiling((view.right - view.left) / lanePeriod) + 3;
  laneY: (common.roadTopY + common.roadBottomY) / 2 - laneDashH / 2;
  laneDashes:
    Range(0, laneDashCount) map (k, idx) =>
    {
      i: laneStartIndex + k;
      x: laneOriginX + i * lanePeriod;
      eval { type: "rect"; name: "lane"; position: [x, laneY]; size: [laneDashW, laneDashH]; fill: "#e2e8f0"; stroke: "none"; width: 0; };
    };

  zebra: common.zebraCrossing;
  roadH: common.roadTopY - common.roadBottomY;
  zebraStripeH: roadH - 2 * zebra.inset;
  zebraStripeY: common.roadBottomY + zebra.inset;
  zebraLeft: zebra.centerX - zebra.width / 2;
  zebraCount: math.Floor(zebra.width / (zebra.stripeWidth + zebra.gap));
  zebraTotal: zebraCount * zebra.stripeWidth + (zebraCount - 1) * zebra.gap;
  zebraScaleY: zebraStripeH / zebraTotal;
  stripeH: zebra.stripeWidth * zebraScaleY;
  gapH: zebra.gap * zebraScaleY;
  zebraStripes:
    Range(0, zebraCount) map (k, idx) =>
    {
      y: zebraStripeY + k * (stripeH + gapH);
      eval { type: "rect"; name: "zebra"; position: [zebraLeft, y]; size: [zebra.width, stripeH]; fill: zebra.fill; stroke: "none"; width: 0; };
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

  eval [skyNight] + starField(sunProgress) + [skyDay, sun, clouds, farHill, hillShade, yard, yardShade, sidewalk, sidewalkShade, farSidewalk, farSidewalkShade, road, laneDashes, zebraStripes, roadShade];
}
