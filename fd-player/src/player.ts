import { FuncDraw } from '@funcdraw/core';
import type { ExpressionLanguage } from '@funcdraw/core';
import { MemoryExpressionResolver } from './resolver';
import { createInlineEntries, loadArchiveContent, normalizeWorkspaceFiles, DEFAULT_VIEW_EXPRESSION } from './loader';
import { interpretGraphics, interpretView, prepareGraphics, renderGraphicsToCanvas, toPlainValue } from './rendering';
import type { ExpressionEntry, FuncdrawPlayerOptions } from './types';
import { createBaseProvider } from './moduleBindings';
import { ModuleRegistry } from './moduleRegistry';

export class FuncdrawPlayer {
  private canvas: HTMLCanvasElement;
  private width?: number;
  private height?: number;
  private background?: string;
  private gridColor?: string;
  private padding?: number;
  private time?: number;
  private resolver: MemoryExpressionResolver | null = null;
  private mainPath: string[] = ['main'];
  private viewPath: string[] = ['view'];
  private warnings: string[] = [];
  private moduleRegistry: ModuleRegistry | null = null;

  constructor(options: FuncdrawPlayerOptions) {
    this.canvas = options.canvas;
    this.width = options.width;
    this.height = options.height;
    this.background = options.background;
    this.gridColor = options.gridColor;
    this.padding = options.padding;
    this.time = options.time;
    this.ensureSize();
  }

  async loadArchiveFromUrl(url: string): Promise<void> {
    const response = await fetch(url);
    if (!response.ok) {
      throw new Error(`Failed to fetch FuncDraw model from ${url}`);
    }
    const buffer = await response.arrayBuffer();
    await this.loadArchiveFromBinary(buffer);
  }

  async loadArchiveFromBinary(input: ArrayBuffer | Uint8Array | Blob): Promise<void> {
    const content = await loadArchiveContent(input);
    this.setResolver(content.entries);
    this.moduleRegistry = content.modules.size > 0 ? new ModuleRegistry(content.modules) : null;
  }

  loadFromExpressions(options: { main: string; view?: string; mainLanguage?: ExpressionLanguage; viewLanguage?: ExpressionLanguage }): void {
    const entries = createInlineEntries(options);
    this.setResolver(entries);
    this.moduleRegistry = null;
  }

  loadFromWorkspaceFiles(files: { path: string; content: string }[]): void {
    const entries = normalizeWorkspaceFiles(files);
    this.setResolver(entries);
    this.moduleRegistry = null;
  }

  setTime(seconds: number | undefined): void {
    this.time = typeof seconds === 'number' && Number.isFinite(seconds) ? seconds : undefined;
  }

  setSize(width?: number, height?: number): void {
    this.width = width;
    this.height = height;
    this.ensureSize();
  }

  setAppearance(options: { background?: string; gridColor?: string; padding?: number }): void {
    if (options.background !== undefined) {
      this.background = options.background;
    }
    if (options.gridColor !== undefined) {
      this.gridColor = options.gridColor;
    }
    if (options.padding !== undefined) {
      this.padding = options.padding;
    }
  }

  getWarnings(): string[] {
    return [...this.warnings];
  }

  render(): void {
    if (!this.resolver) {
      throw new Error('No FuncDraw model has been loaded into the player.');
    }
    this.ensureSize();
    const importFn = this.moduleRegistry?.getImportFunction() ?? null;
    const provider = createBaseProvider(importFn);
    const { evaluateExpression } = FuncDraw.evaluate(this.resolver, this.time, { baseProvider: provider });

    const evaluatePath = (path: string[]) => {
      const result = evaluateExpression(path);
      if (!result) {
        throw new Error(`Expression ${path.join('/')} does not exist in the loaded model.`);
      }
      if (result.error) {
        throw new Error(`Failed to evaluate ${path.join('/')}: ${result.error}`);
      }
      return result.value ?? toPlainValue(result.typed);
    };

    const viewValue = evaluatePath(this.viewPath);
    const viewInfo = interpretView(viewValue);
    if (!viewInfo.extent) {
      throw new Error(viewInfo.warning ?? 'View expression is invalid.');
    }

    const graphicsValue = evaluatePath(this.mainPath);
    const { layers, warnings: graphicsWarnings } = interpretGraphics(graphicsValue);
    if (!layers) {
      throw new Error('Graphics expression did not return a valid primitive collection.');
    }

    const prepared = prepareGraphics(viewInfo.extent, layers);
    this.warnings = [viewInfo.warning, ...graphicsWarnings, ...prepared.warnings].filter((warning): warning is string => Boolean(warning));

    renderGraphicsToCanvas(this.canvas, viewInfo.extent, prepared, {
      background: this.background,
      gridColor: this.gridColor,
      padding: this.padding
    });
  }

  private ensureSize() {
    const resolvedWidth = this.width ?? (this.canvas.width || this.canvas.clientWidth || 1280);
    const resolvedHeight = this.height ?? (this.canvas.height || this.canvas.clientHeight || 720);
    if (this.canvas.width !== resolvedWidth) {
      this.canvas.width = resolvedWidth;
    }
    if (this.canvas.height !== resolvedHeight) {
      this.canvas.height = resolvedHeight;
    }
  }

  private setResolver(entries: ExpressionEntry[]) {
    const working = entries.slice();
    const hasView = working.some(
      (entry) => entry.path.length > 0 && entry.path[entry.path.length - 1].toLowerCase() === 'view'
    );
    if (!hasView) {
      working.push({
        path: ['view'],
        language: 'funcscript',
        source: DEFAULT_VIEW_EXPRESSION,
        createdAt: working.length
      });
    }
    this.resolver = new MemoryExpressionResolver(working);
    this.warnings = [];
  }
}
