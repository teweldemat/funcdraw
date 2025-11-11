import type { ExpressionLanguage } from '@tewelde/funcdraw';
import { defaultGraphicsExpression, defaultViewExpression } from './graphics';
import type { CustomTabDefinition, CustomFolderDefinition, ExampleDefinition } from './examples';
import {
  createCustomTabsFromDefinitions,
  type CustomFolderState,
  type CustomTabState
} from './workspace';
import type { PersistedSnapshot } from './workspace';

const ensureString = (value: unknown): string | null => {
  if (typeof value !== 'string') {
    return null;
  }
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
};

const resolveLanguage = (
  value: unknown,
  fallback: ExpressionLanguage
): ExpressionLanguage => {
  if (typeof value === 'string') {
    const trimmed = value.trim();
    if (trimmed.length > 0) {
      return trimmed as ExpressionLanguage;
    }
  }
  return fallback;
};

type RawExpressionConfig =
  | string
  | {
      expression?: unknown;
      language?: unknown;
    };

type NormalizedExpressionConfig = {
  expression: string;
  language: ExpressionLanguage;
};

const normalizeExpression = (
  config: RawExpressionConfig | undefined,
  fallbackExpression: string,
  fallbackLanguage: ExpressionLanguage
): NormalizedExpressionConfig => {
  if (typeof config === 'string') {
    return { expression: config, language: fallbackLanguage };
  }
  if (config && typeof config === 'object') {
    const expression = ensureString((config as { expression?: unknown }).expression) ?? fallbackExpression;
    const language = resolveLanguage((config as { language?: unknown }).language, fallbackLanguage);
    return { expression, language };
  }
  return { expression: fallbackExpression, language: fallbackLanguage };
};

type RawTabDefinition = {
  name?: unknown;
  expression?: unknown;
  language?: unknown;
};

type RawFolderDefinition = {
  name?: unknown;
  tabs?: unknown;
  folders?: unknown;
};

type ProjectCatalogEntry = {
  id: string;
  name: string;
  catalogExpression: string;
  catalogLanguage: ExpressionLanguage;
  catalogSvg: string;
  descriptionHtml: string;
};

type WorkspaceDefinition = {
  graphics: NormalizedExpressionConfig;
  view: NormalizedExpressionConfig;
  tabs: CustomTabDefinition[];
  folders: CustomFolderDefinition[];
};

type ProjectDefinition = {
  title: string;
  description: string | null;
  catalogEntries: ProjectCatalogEntry[];
  workspace: WorkspaceDefinition;
  persistState: boolean;
};

const buildTabName = (candidate: string, used: Set<string>): string => {
  const stripped = candidate.replace(/[^A-Za-z0-9_]+/g, '_').replace(/^_+|_+$/g, '');
  const base = stripped.length > 0 ? stripped : 'Model';
  const ensurePrefix = /^[A-Za-z_]/.test(base) ? base : `_${base}`;
  let name = ensurePrefix;
  let counter = 2;
  while (used.has(name.toLowerCase())) {
    name = `${ensurePrefix}_${counter}`;
    counter += 1;
  }
  used.add(name.toLowerCase());
  return name;
};

const normalizeTabDefinitions = (
  value: unknown,
  fallback: ProjectCatalogEntry[]
): CustomTabDefinition[] => {
  const definitions: CustomTabDefinition[] = [];
  if (Array.isArray(value)) {
    for (const entry of value) {
      if (!entry || typeof entry !== 'object') {
        continue;
      }
      const expression = ensureString((entry as RawTabDefinition).expression);
      if (!expression) {
        continue;
      }
      const nameCandidate = ensureString((entry as RawTabDefinition).name) ?? 'Tab';
      const language = resolveLanguage((entry as RawTabDefinition).language, 'funcscript');
      definitions.push({
        name: nameCandidate,
        expression,
        language
      });
    }
  }
  if (definitions.length > 0) {
    return definitions;
  }
  const used = new Set<string>();
  fallback.forEach((entry, index) => {
    const nameSource = entry.name || entry.id || `Model ${index + 1}`;
    const name = buildTabName(nameSource, used);
    definitions.push({
      name,
      expression: entry.catalogExpression,
      language: entry.catalogLanguage
    });
  });
  return definitions;
};

const normalizeFolderDefinitions = (value: unknown): CustomFolderDefinition[] => {
  if (!Array.isArray(value)) {
    return [];
  }
  const normalize = (folders: unknown): CustomFolderDefinition[] => {
    if (!Array.isArray(folders)) {
      return [];
    }
    const result: CustomFolderDefinition[] = [];
    for (const folder of folders) {
      if (!folder || typeof folder !== 'object') {
        continue;
      }
      const name = ensureString((folder as RawFolderDefinition).name);
      if (!name) {
        continue;
      }
      const tabs = normalizeTabDefinitions((folder as RawFolderDefinition).tabs, []);
      const children = normalize((folder as RawFolderDefinition).folders);
      result.push({ name, tabs, folders: children });
    }
    return result;
  };
  return normalize(value);
};

const parseCatalogEntries = (value: unknown): ProjectCatalogEntry[] => {
  if (!Array.isArray(value)) {
    return [];
  }
  const entries: ProjectCatalogEntry[] = [];
  value.forEach((entry, index) => {
    if (!entry || typeof entry !== 'object') {
      return;
    }
    const raw = entry as {
      id?: unknown;
      name?: unknown;
      catalogExpression?: unknown;
      catalogLanguage?: unknown;
      catalogSvg?: unknown;
      descriptionHtml?: unknown;
    };
    const expression = ensureString(raw.catalogExpression);
    const svg = ensureString(raw.catalogSvg) ?? '';
    if (!expression) {
      return;
    }
    const id = ensureString(raw.id) ?? `entry-${index + 1}`;
    const name = ensureString(raw.name) ?? id;
    const catalogLanguage = resolveLanguage(raw.catalogLanguage, 'funcscript');
    const descriptionHtml = ensureString(raw.descriptionHtml) ?? '';
    entries.push({
      id,
      name,
      catalogExpression: expression,
      catalogLanguage,
      catalogSvg: svg,
      descriptionHtml
    });
  });
  return entries;
};

const parseWorkspace = (
  value: unknown,
  entries: ProjectCatalogEntry[]
): WorkspaceDefinition => {
  const fallbackGraphics =
    entries.length > 0
      ? { expression: entries[0].catalogExpression, language: entries[0].catalogLanguage }
      : { expression: defaultGraphicsExpression, language: 'funcscript' as ExpressionLanguage };
  const fallbackView = { expression: defaultViewExpression, language: 'funcscript' as ExpressionLanguage };

  if (!value || typeof value !== 'object') {
    return {
      graphics: fallbackGraphics,
      view: fallbackView,
      tabs: normalizeTabDefinitions(undefined, entries),
      folders: []
    };
  }
  const workspace = value as {
    graphics?: RawExpressionConfig;
    view?: RawExpressionConfig;
    tabs?: unknown;
    folders?: unknown;
    persistState?: unknown;
  };
  return {
    graphics: normalizeExpression(workspace.graphics, fallbackGraphics.expression, fallbackGraphics.language),
    view: normalizeExpression(workspace.view, fallbackView.expression, fallbackView.language),
    tabs: normalizeTabDefinitions(workspace.tabs, entries),
    folders: normalizeFolderDefinitions(workspace.folders)
  };
};

const parseProjectDefinition = (): ProjectDefinition | null => {
  if (typeof __FUNC_PROJECT_CONFIG__ !== 'string' || !__FUNC_PROJECT_CONFIG__) {
    return null;
  }
  try {
    const parsed = JSON.parse(__FUNC_PROJECT_CONFIG__);
    if (!parsed || typeof parsed !== 'object') {
      return null;
    }
    const raw = parsed as {
      title?: unknown;
      description?: unknown;
      catalogEntries?: unknown;
      workspace?: unknown;
      persistState?: unknown;
    };
    const title = ensureString(raw.title) ?? 'FuncDraw Project';
    const description = ensureString(raw.description);
    const catalogEntries = parseCatalogEntries(raw.catalogEntries);
    const workspace = parseWorkspace(raw.workspace, catalogEntries);
    const persistState = typeof raw.persistState === 'boolean' ? raw.persistState : false;
    return {
      title,
      description,
      catalogEntries,
      workspace,
      persistState
    };
  } catch {
    return null;
  }
};

const parseProjectModules = (): Map<string, unknown> => {
  const modules = new Map<string, unknown>();
  if (typeof __FUNC_PROJECT_MODULES__ !== 'string' || !__FUNC_PROJECT_MODULES__) {
    return modules;
  }
  try {
    const parsed = JSON.parse(__FUNC_PROJECT_MODULES__);
    if (!parsed || typeof parsed !== 'object') {
      return modules;
    }
    for (const [key, value] of Object.entries(parsed as Record<string, unknown>)) {
      if (typeof key === 'string') {
        modules.set(key, value);
      }
    }
  } catch {
    return modules;
  }
  return modules;
};

const clonePlainValue = (value: unknown): unknown => {
  if (value === null || value === undefined) {
    return null;
  }
  const valueType = typeof value;
  if (valueType === 'string' || valueType === 'number' || valueType === 'boolean') {
    return value;
  }
  if (Array.isArray(value)) {
    return value.map((entry) => clonePlainValue(entry));
  }
  if (valueType === 'object') {
    const result: Record<string, unknown> = {};
    for (const [key, entry] of Object.entries(value as Record<string, unknown>)) {
      result[key] = clonePlainValue(entry);
    }
    return result;
  }
  return null;
};

const createImportFunction = (modules: Map<string, unknown>) => {
  if (modules.size === 0) {
    return null;
  }
  const cache = new Map<string, unknown>();
  return (specifier: unknown): unknown => {
    if (typeof specifier !== 'string') {
      throw new Error('Import specifier must be a string.');
    }
    const trimmed = specifier.trim();
    if (!trimmed) {
      throw new Error('Import specifier cannot be empty.');
    }
    if (!modules.has(trimmed)) {
      const available = Array.from(modules.keys()).join(', ');
      throw new Error(
        available
          ? `Module "${trimmed}" is not available. Known modules: ${available}.`
          : `Module "${trimmed}" is not available.`
      );
    }
    if (cache.has(trimmed)) {
      return cache.get(trimmed);
    }
    const cloned = clonePlainValue(modules.get(trimmed));
    cache.set(trimmed, cloned);
    return cloned;
  };
};

type ProjectBootstrap = {
  project: ProjectDefinition;
  snapshot: PersistedSnapshot;
  workspace: { tabs: CustomTabState[]; folders: CustomFolderState[] } | null;
  importFunction: ((specifier: unknown) => unknown) | null;
  examples: ExampleDefinition[];
  persistState: boolean;
};

export const loadProjectBootstrap = (): ProjectBootstrap | null => {
  const project = parseProjectDefinition();
  if (!project) {
    return null;
  }
  const modules = parseProjectModules();
  const importFunction = createImportFunction(modules);
  const customTabs = createCustomTabsFromDefinitions(project.workspace.tabs);
  const workspaceState = {
    tabs: customTabs,
    folders: [] as CustomFolderState[]
  };
  const snapshot: PersistedSnapshot = {
    selectedExampleId: 'custom',
    projectName: project.title,
    graphicsExpression: project.workspace.graphics.expression,
    graphicsLanguage: project.workspace.graphics.language,
    viewExpression: project.workspace.view.expression,
    viewLanguage: project.workspace.view.language,
    customTabs,
    customFolders: workspaceState.folders,
    activeExpressionTab: 'main'
  };
  return {
    project,
    snapshot,
    workspace: workspaceState,
    importFunction,
    examples: [],
    persistState: project.persistState
  };
};
