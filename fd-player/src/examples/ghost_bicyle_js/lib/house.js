// House where sizeParam controls the main wall height (absolute units)
return (position, sizeParam, kindParam) => {
  const base = position ?? [0, 0];
  const kind = kindParam ?? 'house';
  const isBuilding = kind === 'building';
  const isTower = kind === 'tower';

  const kindScale = isBuilding ? 1.15 : isTower ? 1.05 : 1;
  const baseBodyHeight = isBuilding ? 12 : isTower ? 16 : 7;
  const baseBodyWidth = isBuilding ? 12 : isTower ? 6 : 9;
  const defaultBodyHeight = baseBodyHeight * 1.8 * kindScale;
  const targetBodyHeight = sizeParam ?? defaultBodyHeight;
  const scale = targetBodyHeight / baseBodyHeight;

  const bodyWidth = baseBodyWidth * scale;
  const bodyHeight = targetBodyHeight;
  const bodyLeft = base[0] - bodyWidth / 2;
  const bodyBottom = base[1];
  const bodyTop = base[1] + bodyHeight;

  const bodyColor = isBuilding ? '#475569' : isTower ? '#94a3b8' : '#f97316';
  const accentColor = isBuilding ? '#cbd5f5' : isTower ? '#1e293b' : '#fed7aa';

  const body = {
    type: 'rect',
    data: {
      position: [bodyLeft, bodyBottom],
      size: [bodyWidth, bodyHeight],
      fill: bodyColor,
      stroke: '#0f172a',
      width: 0.35
    }
  };

  const roof = isBuilding
    ? {
        type: 'rect',
        data: {
          position: [bodyLeft - 0.4 * scale, bodyTop],
          size: [bodyWidth + 0.8 * scale, 1.3 * scale],
          fill: '#1f2937',
          stroke: '#0f172a',
          width: 0.35
        }
      }
    : isTower
    ? {
        type: 'polygon',
        data: {
          points: [
            [base[0], bodyTop + 3 * scale],
            [bodyLeft - 1.2 * scale, bodyTop],
            [bodyLeft + bodyWidth + 1.2 * scale, bodyTop]
          ],
          fill: '#334155',
          stroke: '#0f172a',
          width: 0.35
        }
      }
    : {
        type: 'polygon',
        data: {
          points: [
            [bodyLeft - 0.5 * scale, bodyTop],
            [bodyLeft + bodyWidth / 2, bodyTop + 3 * scale],
            [bodyLeft + bodyWidth + 0.5 * scale, bodyTop]
          ],
          fill: '#7c2d12',
          stroke: '#0f172a',
          width: 0.35
        }
      };

  const doorWidth = 2.3 * scale;
  const doorHeight = 3.2 * scale;
  const doorway = {
    doorWidth,
    doorHeight,
    type: 'rect',
    data: {
      position: [base[0] - doorWidth / 2, bodyBottom],
      size: [doorWidth, doorHeight],
      fill: accentColor,
      stroke: '#0f172a',
      width: 0.25
    }
  };

  const windowRect = (centerX, bottomY) => ({
    type: 'rect',
    data: {
      position: [centerX - (2 * scale) / 2, bottomY],
      size: [2 * scale, 2 * scale],
      fill: accentColor,
      stroke: '#0f172a',
      width: 0.2
    }
  });

  const windows = isBuilding
    ? [
        windowRect(bodyLeft + 1.6 * scale, bodyBottom + 2.2 * scale),
        windowRect(bodyLeft + bodyWidth - 1.6 * scale, bodyBottom + 2.2 * scale),
        windowRect(bodyLeft + 1.6 * scale, bodyBottom + 6 * scale),
        windowRect(bodyLeft + bodyWidth - 1.6 * scale, bodyBottom + 6 * scale)
      ]
    : isTower
    ? [
        {
          type: 'circle',
          data: {
            center: [base[0], bodyBottom + bodyHeight / 2],
            radius: 1.6 * scale,
            fill: accentColor,
            stroke: '#0f172a',
            width: 0.2
          }
        }
      ]
    : [
        windowRect(bodyLeft + 2.4 * scale, bodyBottom + 2.4 * scale),
        windowRect(bodyLeft + bodyWidth - 2.4 * scale, bodyBottom + 2.4 * scale)
      ];

  return [body, roof, doorway, windows];
};
