import JSZip from 'jszip';
import type { ExpressionLanguage } from '@funcdraw/core';
import type { ExpressionEntry, WorkspaceFile } from './types';

const EXPRESSION_EXTENSIONS = new Set(['.fs', '.js']);

const normalizePath = (value: string): string[] =>
  value
    .replace(/\\/g, '/')
    .split('/')
    .map((segment) => segment.trim())
    .filter((segment) => segment.length > 0 && segment !== '.' && segment !== '..');

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

export const normalizeWorkspaceFiles = (files: WorkspaceFile[]): ExpressionEntry[] => {
  const entries: ExpressionEntry[] = [];
  const normalized = files
    .map((file) => ({ segments: normalizePath(file.path), content: file.content }))
    .filter((entry) => entry.segments.length > 0);
  const stripped = stripCommonRoot(normalized.map((entry) => entry.segments));
  stripped.forEach((segments, index) => {
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

export const loadArchiveEntries = async (
  input: ArrayBuffer | Uint8Array | Blob
): Promise<ExpressionEntry[]> => {
  const zip = await JSZip.loadAsync(input as any);
  const files: WorkspaceFile[] = [];
  const zipFiles = Object.values(zip.files);
  for (const entry of zipFiles) {
    if (entry.dir) {
      continue;
    }
    const content = await entry.async('string');
    files.push({ path: entry.name, content });
  }
  return normalizeWorkspaceFiles(files);
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
