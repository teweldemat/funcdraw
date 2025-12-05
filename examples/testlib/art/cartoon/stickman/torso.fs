(configInput)=>
{
  defaults:{ fill:"#1f2937"; stroke:"#cbd5f5"; strokeWidth:0.6 };

  config:helpers.normalizeInput(configInput ?? {}, {});
  centerBottomPoint:helpers.normalizePoint(config.centerBottomPoint, [20,6]);
  width:helpers.resolveNumber(config.width, 6);
  height:helpers.resolveNumber(config.height, 11);
  shoulderExtension:math.max(helpers.resolveNumber(config.shoulderExtension, width * 0.1), 0);
  stroke:config.stroke ?? defaults.stroke;
  strokeWidth:config.strokeWidth ?? defaults.strokeWidth;
  direction:normalizeDirection(config.direction, "front");
  isProfile:direction = "left" or direction = "right";

  centerX:centerBottomPoint[0];
  bottomY:centerBottomPoint[1];
  halfWidth:width / 2;
  topY:bottomY + height;

  defaultHandsY:topY - height * 0.15;
  handOffset:halfWidth + shoulderExtension;
  handAttachments:{
    left:helpers.normalizePoint(config.handAttachmentPoints?.left, if isProfile then [centerX, defaultHandsY] else [centerX - handOffset, defaultHandsY]);
    right:helpers.normalizePoint(config.handAttachmentPoints?.right, if isProfile then [centerX, defaultHandsY] else [centerX + handOffset, defaultHandsY]);
  };

  legOffset:width * 0.25;
  legAttachments:{
    left:helpers.normalizePoint(config.legAttachmentPoints?.left, if isProfile then [centerX, bottomY] else [centerX - legOffset, bottomY]);
    right:helpers.normalizePoint(config.legAttachmentPoints?.right, if isProfile then [centerX, bottomY] else [centerX + legOffset, bottomY]);
  };

  headAttachment:helpers.normalizePoint(config.headAttachmentPoint, [centerX, topY]);
  legMidpoint:[
    (legAttachments.left[0] + legAttachments.right[0]) / 2,
    (legAttachments.left[1] + legAttachments.right[1]) / 2
  ];

  graphics:[
    {
      type:"line";
      from:legAttachments.left;
      to:legAttachments.right;
      stroke:stroke;
      width:strokeWidth;
    },
    {
      type:"line";
      from:handAttachments.left;
      to:handAttachments.right;
      stroke:stroke;
      width:strokeWidth;
    },
    {
      type:"line";
      from:headAttachment;
      to:legMidpoint;
      stroke:stroke;
      width:strokeWidth;
    }
  ];

  eval { graphics:graphics };

  normalizeDirection:(value, fallback)=> {
    defaultDir:fallback ?? "front";
    eval if value = null then defaultDir else {
      lowered:text.lower(format(value));
      eval if lowered = "front" then "front"
      else if lowered = "back" then "back"
      else if lowered = "left" then "left"
      else if lowered = "right" then "right"
      else defaultDir;
    };
  };
}
