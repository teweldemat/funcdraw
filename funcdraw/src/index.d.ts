import type { DefaultFsDataProvider, FsDataProvider, TypedValue } from '@tewelde/funcscript/browser';

type PathSegments = string[];

export type ExpressionLanguage = 'funcscript' | 'javascript' | (string & {});

export type ExpressionListItem = {
  kind: 'folder' | 'expression';
  name: string;
  createdAt?: number;
  language?: ExpressionLanguage;
};

export type FuncDrawErrorLocation = {
  index: number;
  line: number;
  column: number;
  length: number;
};

export type FuncDrawErrorContext = {
  lineText: string;
  pointerText: string;
};

export type FuncDrawErrorDetail = {
  kind: 'funcdraw' | 'funcscript-parser' | 'funcscript-runtime' | 'javascript' | (string & {});
  message: string;
  location?: FuncDrawErrorLocation | null;
  context?: FuncDrawErrorContext | null;
  stack?: string | null;
};

export interface ExpressionCollectionResolver {
  listItems(path: PathSegments): ExpressionListItem[];
  getExpression(path: PathSegments): string | null;
}

export type EvaluationResult = {
  value: unknown;
  typed: TypedValue | null;
  error: string | null;
  errorDetails: FuncDrawErrorDetail[] | null;
};

export type EvaluateOptions = {
  baseProvider?: DefaultFsDataProvider;
  timeName?: string;
};

export type FuncDrawEvaluation = {
  environmentProvider: FsDataProvider & {
    setNamedValue(name: string, value: TypedValue | null): void;
  };
  evaluateExpression(path: PathSegments): EvaluationResult | null;
  getFolderValue(path: PathSegments): TypedValue;
  listExpressions(): Array<{ path: PathSegments; name: string }>;
  listFolders(path: PathSegments): Array<{ path: PathSegments; name: string }>;
  setTime(time?: number): void;
};

export declare const FuncDraw: {
  evaluate(
    resolver: ExpressionCollectionResolver,
    time?: number,
    options?: EvaluateOptions
  ): FuncDrawEvaluation;
};

export declare function evaluate(
  resolver: ExpressionCollectionResolver,
  time?: number,
  options?: EvaluateOptions
): FuncDrawEvaluation;

export default FuncDraw;
