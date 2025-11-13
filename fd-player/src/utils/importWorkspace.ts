import JSZip from 'jszip';
import type { ExpressionLanguage } from '@funcdraw/core';
import type { CustomFolderState, CustomTabState } from '../workspace';
import { createCustomFolderId, createCustomTabId, isValidTabName } from '../workspace';

export type WorkspaceImportResult = {
  projectName: string | null;
  graphicsExpression: string | null;
  graphicsLanguage: ExpressionLanguage | null;
  viewExpression: string | null;
  viewLanguage: ExpressionLanguage | null;
  tabs: CustomTabState[];
  folders: CustomFolderState[];
};

const ROOT_KEY = '__root__';

const sanitizeFolderName = (segment: string): string => {
  const trimmed = segment.trim().replace(/^[./]+|[./]+$/g, '');
  if (!trimmed) {
    return 'Folder';
  }
  const cleaned = trimmed.replace(/[^A-Za-z0-9 _-]+/g, ' ').replace(/\s+/g, ' ').trim();
  if (!cleaned) {
    return 'Folder';
  }
  return cleaned.replace(/\b\w/g, (char) => char.toUpperCase());
};

const sanitizeTabBaseName = (value: string): string => value.replace(/[^A-Za-z0-9_]/g, '_');

const normalizePathSegments = (value: string): string[] =>
  value
    .replace(/\\/g, '/')
    .replace(/^\.\/+/, '')
    .split('/')
    .map((segment) => segment.trim())
    .filter((segment) => segment.length > 0);

const isExpressionFile = (filename: string): boolean => /\.(fs|js)$/i.test(filename);

const extensionToLanguage = (extension: string): ExpressionLanguage =>
  extension.toLowerCase() === 'js' ? 'javascript' : 'funcscript';

const buildProjectName = (segment: string | null): string | null => {
  if (!segment) {
    return null;
  }
  const friendly = segment.replace(/[-_]+/g, ' ').replace(/\s+/g, ' ').trim();
  return friendly || null;
};

const getUniqueName = (
  registry: Map<string, Set<string>>,
  parentKey: string,
  desired: string,
  fallback: string
): string => {
  const key = parentKey || ROOT_KEY;
  let set = registry.get(key);
  if (!set) {
    set = new Set();
    registry.set(key, set);
  }
  const base = (desired || fallback).trim() || fallback;
  let candidate = base;
  let suffix = 2;
  while (set.has(candidate.toLowerCase())) {
    candidate = `${base} ${suffix}`;
    suffix += 1;
  }
  set.add(candidate.toLowerCase());
  return candidate;
};

const buildTabNameGenerator = (registry: Map<string, Set<string>>) => {
  let fallbackCounter = 1;
  return (parentKey: string, desired: string): string => {
    const key = parentKey || ROOT_KEY;
    let set = registry.get(key);
    if (!set) {
      set = new Set();
      registry.set(key, set);
    }
    let base = sanitizeTabBaseName(desired);
    if (!base || !isValidTabName(base)) {
      base = `expression_${fallbackCounter++}`;
    }
    let candidate = base;
    let suffix = 2;
    while (set.has(candidate.toLowerCase()) || !isValidTabName(candidate)) {
      candidate = `${base}_${suffix}`;
      suffix += 1;
    }
    set.add(candidate.toLowerCase());
    return candidate;
  };
};

export const importWorkspaceFromArchive = async (file: File | Blob): Promise<WorkspaceImportResult> => {
  const zip = await JSZip.loadAsync(file);
  const entries = await Promise.all(
    Object.values(zip.files)
      .filter((entry) => !entry.dir)
      .map(async (entry) => ({
        rawPath: entry.name,
        content: await entry.async('string')
      }))
  );

  const parsedPaths = entries
    .map((entry) => ({ segments: normalizePathSegments(entry.rawPath), content: entry.content }))
    .filter((entry) => entry.segments.length > 0 && !entry.segments[0].startsWith('__MACOSX'));

  if (parsedPaths.length === 0) {
    throw new Error('Archive does not contain any FuncDraw expressions.');
  }

  let projectNameSegment: string | null = null;
  const rootCandidate = parsedPaths[0].segments[0];
  if (parsedPaths.every((entry) => entry.segments[0] === rootCandidate)) {
    projectNameSegment = rootCandidate;
    parsedPaths.forEach((entry) => {
      entry.segments.shift();
    });
  }

  const filteredEntries = parsedPaths
    .map((entry) => ({ segments: entry.segments.filter((segment) => segment.length > 0), content: entry.content }))
    .filter((entry) => entry.segments.length > 0);

  filteredEntries.sort((a, b) => a.segments.join('/').localeCompare(b.segments.join('/')));

  const folders: CustomFolderState[] = [];
  const tabs: CustomTabState[] = [];
  let folderOrder = 0;
  let tabOrder = 0;
  const folderNameRegistry = new Map<string, Set<string>>();
  const tabNameRegistry = new Map<string, Set<string>>();
  const folderPathMap = new Map<string, CustomFolderState>();
  const folderIdToPathKey = new Map<string, string>();
  const folderSegmentCache = new Map<string, string>();
  const getTabName = buildTabNameGenerator(tabNameRegistry);

  const ensureFolder = (folderSegments: string[]): { folderId: string | null; pathKey: string } => {
    if (folderSegments.length === 0) {
      return { folderId: null, pathKey: ROOT_KEY };
    }
    let parentId: string | null = null;
    let currentPathKey = '';
    for (const rawSegment of folderSegments) {
      const parentKey = currentPathKey || ROOT_KEY;
      const segmentKey = `${parentKey}::${rawSegment.trim().toLowerCase()}`;
      let folderName = folderSegmentCache.get(segmentKey);
      if (!folderName) {
        const sanitized = sanitizeFolderName(rawSegment);
        folderName = getUniqueName(folderNameRegistry, parentKey, sanitized, 'Folder');
        folderSegmentCache.set(segmentKey, folderName);
      }
      const fragment = folderName.trim().toLowerCase().replace(/\s+/g, '-');
      const nextPathKey = currentPathKey ? `${currentPathKey}/${fragment}` : fragment;
      let folderState = folderPathMap.get(nextPathKey);
      if (!folderState) {
        folderState = {
          id: createCustomFolderId(),
          name: folderName,
          parentId,
          createdAt: folderOrder++
        };
        folders.push(folderState);
        folderPathMap.set(nextPathKey, folderState);
      }
      folderIdToPathKey.set(folderState.id, nextPathKey);
      parentId = folderState.id;
      currentPathKey = nextPathKey;
    }
    return { folderId: parentId, pathKey: currentPathKey || ROOT_KEY };
  };

  let graphicsExpression: string | null = null;
  let graphicsLanguage: ExpressionLanguage | null = null;
  let viewExpression: string | null = null;
  let viewLanguage: ExpressionLanguage | null = null;

  for (const entry of filteredEntries) {
    const segments = entry.segments;
    const filename = segments[segments.length - 1];
    if (!isExpressionFile(filename)) {
      continue;
    }
    const extension = filename.split('.').pop() ?? 'fs';
    const language = extensionToLanguage(extension);
    const baseName = filename.replace(/\.[^.]+$/, '');
    const depth = segments.length;
    const isRootMain = depth === 1 && baseName.toLowerCase() === 'main';
    const isRootView = depth === 1 && baseName.toLowerCase() === 'view';
    if (isRootMain) {
      graphicsExpression = entry.content;
      graphicsLanguage = language;
      continue;
    }
    if (isRootView) {
      viewExpression = entry.content;
      viewLanguage = language;
      continue;
    }
    const folderSegments = segments.slice(0, -1);
    const { folderId, pathKey } = ensureFolder(folderSegments);
    const parentKey = folderId ? folderIdToPathKey.get(folderId) ?? ROOT_KEY : ROOT_KEY;
    const tabName = getTabName(parentKey, baseName);
    tabs.push({
      id: createCustomTabId(),
      name: tabName,
      expression: entry.content,
      folderId,
      createdAt: tabOrder++,
      language
    });
  }

  return {
    projectName: buildProjectName(projectNameSegment),
    graphicsExpression,
    graphicsLanguage,
    viewExpression,
    viewLanguage,
    tabs,
    folders
  };
};
