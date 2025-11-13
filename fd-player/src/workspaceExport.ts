import type { ExpressionLanguage } from '@funcdraw/core';
import type { CustomFolderState, CustomTabState } from './workspace';

export type WorkspaceFile = {
  path: string;
  content: string;
};

type FolderNode = {
  folder: CustomFolderState;
  children: FolderNode[];
};

const sanitizeSegment = (raw: string, fallback: string): string => {
  const trimmed = raw.trim();
  if (!trimmed) {
    return fallback;
  }
  const normalized = trimmed.replace(/[^A-Za-z0-9-_ ]+/g, '_').replace(/\s+/g, '_');
  return normalized || fallback;
};

const getUniqueName = (
  parentKey: string,
  desired: string,
  fallback: string,
  registry: Map<string, Set<string>>
): string => {
  const key = parentKey;
  const existing = registry.get(key) ?? new Set<string>();
  if (!registry.has(key)) {
    registry.set(key, existing);
  }
  const base = sanitizeSegment(desired, fallback) || fallback;
  let candidate = base;
  let suffix = 1;
  while (existing.has(candidate.toLowerCase())) {
    candidate = `${base}_${suffix}`;
    suffix += 1;
  }
  existing.add(candidate.toLowerCase());
  return candidate;
};

const buildFolderTree = (folders: CustomFolderState[]): FolderNode[] => {
  const byId = new Map<string, FolderNode>();
  const roots: FolderNode[] = [];
  for (const folder of folders) {
    byId.set(folder.id, { folder, children: [] });
  }
  for (const node of byId.values()) {
    const parentId = node.folder.parentId;
    if (parentId && byId.has(parentId)) {
      byId.get(parentId)!.children.push(node);
    } else {
      roots.push(node);
    }
  }
  const sortNodes = (list: FolderNode[]) => {
    list.sort((a, b) => (a.folder.createdAt ?? 0) - (b.folder.createdAt ?? 0));
    list.forEach((child) => sortNodes(child.children));
  };
  sortNodes(roots);
  return roots;
};

const getExtension = (language: ExpressionLanguage | undefined) =>
  language === 'javascript' ? '.js' : '.fs';

export type WorkspaceExportConfig = {
  graphicsExpression: string;
  graphicsLanguage: ExpressionLanguage;
  viewExpression: string;
  viewLanguage: ExpressionLanguage;
  tabs: CustomTabState[];
  folders: CustomFolderState[];
};

export const buildWorkspaceFiles = ({
  graphicsExpression,
  graphicsLanguage,
  viewExpression,
  viewLanguage,
  tabs,
  folders
}: WorkspaceExportConfig): WorkspaceFile[] => {
  const files: WorkspaceFile[] = [];

  files.push({ path: `main${getExtension(graphicsLanguage)}`, content: graphicsExpression });
  files.push({ path: `view${getExtension(viewLanguage)}`, content: viewExpression });

  if ((!tabs || tabs.length === 0) && (!folders || folders.length === 0)) {
    return files;
  }

  const folderTree = buildFolderTree(folders);
  const folderPaths = new Map<string, string>();
  const folderNameRegistry = new Map<string, Set<string>>();

  const assignFolderPaths = (nodes: FolderNode[], parentPath: string) => {
    for (const node of nodes) {
      const segment = getUniqueName(parentPath || '__root__', node.folder.name, 'folder', folderNameRegistry);
      const currentPath = parentPath ? `${parentPath}/${segment}` : segment;
      folderPaths.set(node.folder.id, currentPath);
      assignFolderPaths(node.children, currentPath);
    }
  };

  assignFolderPaths(folderTree, '');

  const fileNameRegistry = new Map<string, Set<string>>();

  const appendTabFile = (tab: CustomTabState) => {
    const parentPath = tab.folderId ? folderPaths.get(tab.folderId) ?? '' : '';
    const registryKey = parentPath || '__root__';
    const sanitizedBase = getUniqueName(registryKey, tab.name, 'expression', fileNameRegistry);
    const relative = parentPath ? `${parentPath}/${sanitizedBase}${getExtension(tab.language)}` : `${sanitizedBase}${getExtension(tab.language)}`;
    files.push({ path: relative, content: tab.expression });
  };

  const sortedTabs = [...tabs].sort((a, b) => (a.createdAt ?? 0) - (b.createdAt ?? 0));
  sortedTabs.forEach(appendTabFile);

  return files;
};
