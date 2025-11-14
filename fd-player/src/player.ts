import { FuncDraw } from '@funcdraw/core';
import type { ExpressionLanguage } from '@funcdraw/core';
import type { ExpressionCollectionResolver, ExpressionListItem } from '@funcdraw/core';
import { interpretGraphics, interpretView, prepareGraphics, renderGraphicsToCanvas, toPlainValue } from './rendering.js';
import type { FuncdrawPlayerOptions } from './types.js';
import { createBaseProvider } from './moduleBindings.js';
import { ModuleRegistry, type ModuleEntryMap } from './moduleRegistry.js';
import { DEFAULT_VIEW_EXPRESSION } from './loader.js';

export class FuncdrawPlayer {
  private canvas: HTMLCanvasElement;
  private width?: number;
  private height?: number;
  private background?: string;
  private gridColor?: string;
  private padding?: number;
  private time?: number;
  private resolver: ExpressionCollectionResolver | null = null;
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

  setResolver(resolver: ExpressionCollectionResolver, modules?: ModuleEntryMap | ModuleRegistry | null): void {
    this.resolver = this.ensureViewResolver(resolver);
    this.assignModuleRegistry(modules ?? null);
    this.warnings = [];
  }

  setModuleEntries(modules: ModuleEntryMap | null): void {
    this.assignModuleRegistry(modules);
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
      throw new Error('No FuncDraw resolver has been configured for the player.');
    }
    this.ensureSize();
    const importFn = this.moduleRegistry?.getImportFunction() ?? null;
    const provider = createBaseProvider(importFn);
    const { evaluateExpression } = FuncDraw.evaluate(this.resolver, this.time, { baseProvider: provider });

    const evaluatePath = (path: string[]) => {
      console.log('[FuncdrawPlayer] evaluatePath:start', { path });
      const result = evaluateExpression(path);
      if (!result) {
        throw new Error(`Expression ${path.join('/')} does not exist in the loaded model.`);
      }
      if (result.error) {
        throw new Error(`Failed to evaluate ${path.join('/')}: ${result.error}`);
      }
      const resolved = result.value ?? toPlainValue(result.typed);
      console.log('[FuncdrawPlayer] evaluatePath:result', {
        path,
        typeofValue: typeof resolved,
        constructorName: resolved?.constructor?.name,
        hasTyped: Boolean(result.typed),
        hasValue: 'value' in result && result.value !== undefined
      });
      return resolved;
    };

    const viewValue = evaluatePath(this.viewPath);
    console.log('[FuncdrawPlayer] interpretView:start', { viewPath: this.viewPath });
    const viewInfo = interpretView(viewValue);
    console.log('[FuncdrawPlayer] interpretView:result', {
      warning: viewInfo.warning,
      hasExtent: Boolean(viewInfo.extent)
    });
    if (!viewInfo.extent) {
      throw new Error(viewInfo.warning ?? 'View expression is invalid.');
    }

    const graphicsValue = evaluatePath(this.mainPath);
    console.log('[FuncdrawPlayer] interpretGraphics:start', { mainPath: this.mainPath });
    const { layers, warnings: graphicsWarnings } = interpretGraphics(graphicsValue);
    console.log('[FuncdrawPlayer] interpretGraphics:result', {
      layerCount: layers?.length ?? 0,
      warnings: graphicsWarnings
    });
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

  private assignModuleRegistry(modules: ModuleEntryMap | ModuleRegistry | null) {
    if (!modules) {
      this.moduleRegistry = null;
      return;
    }
    if (modules instanceof ModuleRegistry) {
      this.moduleRegistry = modules;
      return;
    }
    this.moduleRegistry = modules.size > 0 ? new ModuleRegistry(modules) : null;
  }

  private ensureViewResolver(resolver: ExpressionCollectionResolver): ExpressionCollectionResolver {
    const hasView = resolver
      .listItems([])
      .some((entry) => entry.kind === 'expression' && entry.name.toLowerCase() === 'view');
    if (hasView) {
      return resolver;
    }
    return new ViewFallbackResolver(resolver);
  }
}

class ViewFallbackResolver implements ExpressionCollectionResolver {
  constructor(private readonly base: ExpressionCollectionResolver) {}

  listItems(segments: string[]): ExpressionListItem[] {
    const items = this.base.listItems(segments);
    if (segments.length === 0 && !items.some((entry) => entry.kind === 'expression' && entry.name.toLowerCase() === 'view')) {
      return [
        ...items,
        {
          kind: 'expression',
          name: 'view',
          createdAt: Number.MAX_SAFE_INTEGER,
          language: 'funcscript'
        }
      ];
    }
    return items;
  }

  getExpression(segments: string[]): string | null {
    if (segments.length === 1 && segments[0].toLowerCase() === 'view') {
      return this.base.getExpression(segments) ?? DEFAULT_VIEW_EXPRESSION;
    }
    return this.base.getExpression(segments);
  }
}
