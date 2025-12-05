(configInput)=>
{
  defaults:{ fill:"#1f2937"; stroke:"#cbd5f5"; strokeWidth:0.6 };

  config:configInput ?? {};
  centerBottomPoint:if config.centerBottomPoint = null then [20,6] else config.centerBottomPoint;
  width:if config.width = null then 6 else config.width;
  height:if config.height = null then 11 else config.height;
  shoulderExtension:math.max(if config.shoulderExtension = null then width * 0.1 else config.shoulderExtension, 0);
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
    left:if config.handAttachmentPoints?.left = null then (if isProfile then [centerX, defaultHandsY] else [centerX - handOffset, defaultHandsY]) else config.handAttachmentPoints.left;
    right:if config.handAttachmentPoints?.right = null then (if isProfile then [centerX, defaultHandsY] else [centerX + handOffset, defaultHandsY]) else config.handAttachmentPoints.right;
  };

  legOffset:width * 0.25;
  legAttachments:{
    left:if config.legAttachmentPoints?.left = null then (if isProfile then [centerX, bottomY] else [centerX - legOffset, bottomY]) else config.legAttachmentPoints.left;
    right:if config.legAttachmentPoints?.right = null then (if isProfile then [centerX, bottomY] else [centerX + legOffset, bottomY]) else config.legAttachmentPoints.right;
  };

  headAttachment:if config.headAttachmentPoint = null then [centerX, topY] else config.headAttachmentPoint;
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
