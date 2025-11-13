import type {
  ExpressionCollectionResolver,
  ExpressionLanguage,
  ExpressionListItem
} from '@funcdraw/core';
import type { ExpressionEntry } from './types';

const ROOT_SEGMENT = '__root__';

type ExpressionNode = {
  name: string;
  lower: string;
  language: ExpressionLanguage;
  source: string;
  createdAt: number;
};

type FolderNode = {
  name: string;
  lower: string;
  createdAt: number;
  folders: Map<string, FolderNode>;
  expressions: Map<string, ExpressionNode>;
};

const createFolderNode = (name: string, createdAt: number): FolderNode => ({
  name,
  lower: name.toLowerCase(),
  createdAt,
  folders: new Map(),
  expressions: new Map()
});

export class MemoryExpressionResolver implements ExpressionCollectionResolver {
  private readonly root: FolderNode;
  private sequence = 1;

  constructor(entries: ExpressionEntry[]) {
    this.root = createFolderNode(ROOT_SEGMENT, 0);
    entries.forEach((entry) => {
      this.insert(entry);
    });
  }

  listItems(segments: string[]): ExpressionListItem[] {
    const folder = this.resolveFolder(segments);
    if (!folder) {
      return [];
    }
    const items: ExpressionListItem[] = [];
    folder.folders.forEach((child) => {
      items.push({ kind: 'folder', name: child.name, createdAt: child.createdAt });
    });
    folder.expressions.forEach((expression) => {
      items.push({
        kind: 'expression',
        name: expression.name,
        createdAt: expression.createdAt,
        language: expression.language
      });
    });
    items.sort((a, b) => (a.createdAt ?? 0) - (b.createdAt ?? 0));
    return items;
  }

  getExpression(segments: string[]): string | null {
    if (!segments || segments.length === 0) {
      return null;
    }
    const folderPath = segments.slice(0, -1);
    const targetName = segments[segments.length - 1].toLowerCase();
    const folder = this.resolveFolder(folderPath);
    if (!folder) {
      return null;
    }
    const expression = folder.expressions.get(targetName);
    return expression ? expression.source : null;
  }

  private resolveFolder(segments: string[]): FolderNode | null {
    if (!segments || segments.length === 0) {
      return this.root;
    }
    let current: FolderNode = this.root;
    for (const rawSegment of segments) {
      const key = rawSegment.trim().toLowerCase();
      if (!key) {
        return null;
      }
      const next: FolderNode | undefined = current.folders.get(key);
      if (!next) {
        return null;
      }
      current = next;
    }
    return current;
  }

  private insert(entry: ExpressionEntry) {
    const segments = entry.path.slice(0, -1);
    const name = entry.path[entry.path.length - 1];
    let current = this.root;
    segments.forEach((segment) => {
      const key = segment.trim();
      if (!key) {
        return;
      }
      const lower = key.toLowerCase();
      let child = current.folders.get(lower);
      if (!child) {
        child = createFolderNode(segment, this.sequence++);
        current.folders.set(lower, child);
      }
      current = child;
    });
    const lowerName = name.toLowerCase();
    current.expressions.set(lowerName, {
      name,
      lower: lowerName,
      language: entry.language,
      source: entry.source,
      createdAt: entry.createdAt
    });
  }
}
