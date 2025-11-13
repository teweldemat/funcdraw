import type { ExpressionLanguage } from '@funcdraw/core';

export type ExpressionEntry = {
  path: string[];
  language: ExpressionLanguage;
  source: string;
  createdAt: number;
};

export type WorkspaceFile = {
  path: string;
  content: string;
};

export type ViewExtent = {
  minX: number;
  minY: number;
  maxX: number;
  maxY: number;
};

export type PlayerAppearance = {
  background?: string;
  gridColor?: string;
  padding?: number;
};

export type FuncdrawPlayerOptions = PlayerAppearance & {
  canvas: HTMLCanvasElement;
  width?: number;
  height?: number;
  time?: number;
};

export type InlineExpressionsOptions = {
  main: string;
  view?: string;
  mainLanguage?: ExpressionLanguage;
  viewLanguage?: ExpressionLanguage;
};

export type GraphicsLayer = Array<PrimitiveLine | PrimitiveRect | PrimitiveCircle | PrimitiveEllipse | PrimitivePolygon | PrimitiveText>;

export type PreparedGraphics = {
  layers: GraphicsLayer[];
  warnings: string[];
};

type Point = [number, number];

type CommonPrimitive = {
  stroke?: string | null;
  fill?: string | null;
  width?: number;
};

export type PrimitiveLine = CommonPrimitive & {
  type: 'line';
  from: Point;
  to: Point;
  dash?: number[] | null;
};

export type PrimitiveRect = CommonPrimitive & {
  type: 'rect';
  position: Point;
  size: Point;
};

export type PrimitiveCircle = CommonPrimitive & {
  type: 'circle';
  center: Point;
  radius: number;
};

export type PrimitiveEllipse = CommonPrimitive & {
  type: 'ellipse';
  center: Point;
  radiusX: number;
  radiusY: number;
};

export type PrimitivePolygon = CommonPrimitive & {
  type: 'polygon';
  points: Point[];
};

export type PrimitiveText = {
  type: 'text';
  position: Point;
  text: string;
  color?: string;
  fontSize: number;
  align: 'left' | 'right' | 'center';
};
