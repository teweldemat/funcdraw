import JSZip from 'jszip';
import type { ExpressionLanguage } from '@funcdraw/core';
import type { ExpressionEntry, WorkspaceFile } from './types.js';

const EXPRESSION_EXTENSIONS = new Set(['.fs', '.js']);
const MODULE_FOLDER = '__modules__';

const normalizePath = (value: string): string[] =>
  value
    .replace(/\\/g, '/')
    .split('/')
    .map((segment) => segment.trim())
    .filter((segment) => segment.length > 0 && segment !== '.' && segment !== '..');

const decodeModuleSpecifier = (value: string): string | null => {
  try {
    return decodeURIComponent(value);
  } catch {
    return null;
  }
};

const extensionToLanguage = (filename: string): ExpressionLanguage =>
  filename.toLowerCase().endsWith('.js') ? 'javascript' : 'funcscript';

const stripCommonRoot = (segmentsList: string[][]): string[][] => {
  if (segmentsList.length === 0) {
    return segmentsList;
  }
  if (segmentsList.some((segments) => segments.length === 0)) {
    return segmentsList;
  }
  const rootCandidate = segmentsList[0][0];
  if (!segmentsList.every((segments) => segments[0] === rootCandidate)) {
    return segmentsList;
  }
  return segmentsList.map((segments) => segments.slice(1));
};

export const normalizeWorkspaceFiles = (
  files: WorkspaceFile[],
  options?: { stripCommonRoot?: boolean }
): ExpressionEntry[] => {
  const entries: ExpressionEntry[] = [];
  const normalized = files
    .map((file) => ({ segments: normalizePath(file.path), content: file.content }))
    .filter((entry) => entry.segments.length > 0);
  const shouldStrip = options?.stripCommonRoot !== false;
  const processedSegments = shouldStrip ? stripCommonRoot(normalized.map((entry) => entry.segments)) : normalized.map((entry) => entry.segments);
  processedSegments.forEach((segments, index) => {
    const filename = segments[segments.length - 1];
    const lower = filename.toLowerCase();
    const matches = /\.[^.]+$/.exec(lower);
    const extension = matches ? matches[0] : '';
    if (!EXPRESSION_EXTENSIONS.has(extension)) {
      return;
    }
    const name = filename.replace(/\.[^.]+$/, '');
    const path = segments.slice(0, -1).concat(name);
    if (path.length === 0) {
      path.push(name || 'main');
    }
    entries.push({
      path,
      language: extensionToLanguage(lower),
      source: normalized[index].content,
      createdAt: index
    });
  });
  return entries;
};

export type ArchiveModules = Map<string, ExpressionEntry[]>;

export type ArchiveContent = {
  entries: ExpressionEntry[];
  modules: ArchiveModules;
};

export const loadArchiveContent = async (
  input: ArrayBuffer | Uint8Array | Blob
): Promise<ArchiveContent> => {
  const zip = await JSZip.loadAsync(input as any);
  const workspaceFiles: WorkspaceFile[] = [];
  const moduleFiles = new Map<string, WorkspaceFile[]>();
  let rootPrefix: string | null = null;
  for (const entry of Object.values(zip.files)) {
    if (entry.dir) {
      continue;
    }
    const content = await entry.async('string');
    const segments = normalizePath(entry.name);
    if (segments.length === 0) {
      continue;
    }
    if (!rootPrefix) {
      rootPrefix = segments[0];
    }
    const relative = segments[0] === rootPrefix ? segments.slice(1) : segments;
    if (relative.length === 0) {
      continue;
    }
    if (relative[0] === MODULE_FOLDER) {
      if (relative.length < 3) {
        continue;
      }
      const specifier = decodeModuleSpecifier(relative[1]);
      if (!specifier) {
        continue;
      }
      const modulePath = relative.slice(2).join('/');
      if (!modulePath) {
        continue;
      }
      const list = moduleFiles.get(specifier) ?? [];
      list.push({ path: modulePath, content });
      moduleFiles.set(specifier, list);
      continue;
    }
    workspaceFiles.push({ path: relative.join('/'), content });
  }
  const entries = normalizeWorkspaceFiles(workspaceFiles);
  const modules: ArchiveModules = new Map();
  moduleFiles.forEach((files, specifier) => {
    const normalized = normalizeWorkspaceFiles(files, { stripCommonRoot: false });
    if (normalized.length > 0) {
      modules.set(specifier, normalized);
    }
  });
  return { entries, modules };
};

export const loadArchiveEntries = async (
  input: ArrayBuffer | Uint8Array | Blob
): Promise<ExpressionEntry[]> => {
  const content = await loadArchiveContent(input);
  return content.entries;
};

export const createInlineEntries = (options: {
  main: string;
  view?: string;
  mainLanguage?: ExpressionLanguage;
  viewLanguage?: ExpressionLanguage;
}): ExpressionEntry[] => {
  const entries: ExpressionEntry[] = [];
  entries.push({
    path: ['main'],
    language: options.mainLanguage ?? 'funcscript',
    source: options.main,
    createdAt: 0
  });
  entries.push({
    path: ['view'],
    language: options.viewLanguage ?? 'funcscript',
    source: options.view ?? DEFAULT_VIEW_EXPRESSION,
    createdAt: 1
  });
  return entries;
};

export const DEFAULT_VIEW_EXPRESSION = `{
  return { minX:-10, minY:-10, maxX:10, maxY:10 };
}`;
