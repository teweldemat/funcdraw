import { useEffect, useMemo, useState } from 'react';
import type { WorkspaceImportResult } from '../utils/importWorkspace';
import type { CustomFolderState, CustomTabState } from '../workspace';

type SelectionState = {
  folders: Set<string>;
  tabs: Set<string>;
};

type LibraryImportDialogProps = {
  open: boolean;
  targetName?: string;
  result: WorkspaceImportResult;
  onConfirm: (selection: { folderIds: string[]; tabIds: string[] }) => void;
  onCancel: () => void;
};

const ROOT_KEY = '__root__';

const buildFoldersByParent = (folders: CustomFolderState[]) => {
  const map = new Map<string | null, CustomFolderState[]>();
  for (const folder of folders) {
    const parent = folder.parentId ?? null;
    if (!map.has(parent)) {
      map.set(parent, []);
    }
    map.get(parent)!.push(folder);
  }
  for (const list of map.values()) {
    list.sort((a, b) => (a.createdAt ?? 0) - (b.createdAt ?? 0));
  }
  return map;
};

const buildTabsByFolder = (tabs: CustomTabState[]) => {
  const map = new Map<string | null, CustomTabState[]>();
  for (const tab of tabs) {
    const parent = tab.folderId ?? null;
    if (!map.has(parent)) {
      map.set(parent, []);
    }
    map.get(parent)!.push(tab);
  }
  for (const list of map.values()) {
    list.sort((a, b) => (a.createdAt ?? 0) - (b.createdAt ?? 0));
  }
  return map;
};

export const LibraryImportDialog = ({ open, targetName, result, onConfirm, onCancel }: LibraryImportDialogProps) => {
  const foldersByParent = useMemo(() => buildFoldersByParent(result.folders), [result.folders]);
  const tabsByFolder = useMemo(() => buildTabsByFolder(result.tabs), [result.tabs]);
  const folderParentMap = useMemo(() => {
    const map = new Map<string, string | null>();
    for (const folder of result.folders) {
      map.set(folder.id, folder.parentId ?? null);
    }
    return map;
  }, [result.folders]);
  const tabParentMap = useMemo(() => {
    const map = new Map<string, string | null>();
    for (const tab of result.tabs) {
      map.set(tab.id, tab.folderId ?? null);
    }
    return map;
  }, [result.tabs]);
  const topLevelFolders = foldersByParent.get(null) ?? [];
  const topLevelTabs = tabsByFolder.get(null) ?? [];

  const [selection, setSelection] = useState<SelectionState>(() => ({
    folders: new Set(result.folders.map((folder) => folder.id)),
    tabs: new Set(result.tabs.map((tab) => tab.id))
  }));

  useEffect(() => {
    setSelection({
      folders: new Set(result.folders.map((folder) => folder.id)),
      tabs: new Set(result.tabs.map((tab) => tab.id))
    });
  }, [result]);

  if (!open) {
    return null;
  }

  const updateSelection = (updater: (next: SelectionState) => void) => {
    setSelection((previous) => {
      const next: SelectionState = {
        folders: new Set(previous.folders),
        tabs: new Set(previous.tabs)
      };
      updater(next);
      return next;
    });
  };

  const setFolderAndDescendants = (
    folderId: string,
    checked: boolean,
    next: SelectionState
  ) => {
    if (checked) {
      next.folders.add(folderId);
    } else {
      next.folders.delete(folderId);
    }
    const childTabs = tabsByFolder.get(folderId) ?? [];
    for (const tab of childTabs) {
      if (checked) {
        next.tabs.add(tab.id);
      } else {
        next.tabs.delete(tab.id);
      }
    }
    const childFolders = foldersByParent.get(folderId) ?? [];
    for (const child of childFolders) {
      setFolderAndDescendants(child.id, checked, next);
    }
  };

  const syncParentSelection = (folderId: string | null, next: SelectionState) => {
    if (!folderId) {
      return;
    }
    const childFolders = foldersByParent.get(folderId) ?? [];
    const childTabs = tabsByFolder.get(folderId) ?? [];
    const allFoldersSelected = childFolders.every((child) => next.folders.has(child.id));
    const allTabsSelected = childTabs.every((tab) => next.tabs.has(tab.id));
    if (childFolders.length === 0 && childTabs.length === 0) {
      next.folders.delete(folderId);
    } else if (allFoldersSelected && allTabsSelected) {
      next.folders.add(folderId);
    } else {
      next.folders.delete(folderId);
    }
    syncParentSelection(folderParentMap.get(folderId) ?? null, next);
  };

  const handleFolderToggle = (folderId: string, checked: boolean) => {
    updateSelection((next) => {
      setFolderAndDescendants(folderId, checked, next);
      syncParentSelection(folderParentMap.get(folderId) ?? null, next);
    });
  };

  const handleTabToggle = (tabId: string, checked: boolean) => {
    updateSelection((next) => {
      if (checked) {
        next.tabs.add(tabId);
      } else {
        next.tabs.delete(tabId);
      }
      const folderId = tabParentMap.get(tabId) ?? null;
      if (folderId) {
        const childFolders = foldersByParent.get(folderId) ?? [];
        const childTabs = tabsByFolder.get(folderId) ?? [];
        const allFoldersSelected = childFolders.every((child) => next.folders.has(child.id));
        const allTabsSelected = childTabs.every((tab) => next.tabs.has(tab.id));
        if (allFoldersSelected && allTabsSelected && childFolders.length + childTabs.length > 0) {
          next.folders.add(folderId);
        } else {
          next.folders.delete(folderId);
        }
        syncParentSelection(folderParentMap.get(folderId) ?? null, next);
      }
    });
  };

  const renderTab = (tab: CustomTabState) => (
    <li key={tab.id} className="library-import-node">
      <label>
        <input
          type="checkbox"
          checked={selection.tabs.has(tab.id)}
          onChange={(event) => handleTabToggle(tab.id, event.target.checked)}
        />
        <span>{tab.name}</span>
      </label>
    </li>
  );

  const renderFolder = (folder: CustomFolderState) => {
    const childFolders = foldersByParent.get(folder.id) ?? [];
    const childTabs = tabsByFolder.get(folder.id) ?? [];
    const isChecked = selection.folders.has(folder.id);
    return (
      <li key={folder.id} className="library-import-node">
        <label>
          <input
            type="checkbox"
            checked={isChecked}
            onChange={(event) => handleFolderToggle(folder.id, event.target.checked)}
          />
          <span>{folder.name}</span>
        </label>
        {(childFolders.length > 0 || childTabs.length > 0) && (
          <ul>
            {childTabs.map((tab) => renderTab(tab))}
            {childFolders.map((child) => renderFolder(child))}
          </ul>
        )}
      </li>
    );
  };

  const handleConfirm = () => {
    if (selection.folders.size === 0 && selection.tabs.size === 0) {
      return;
    }
    onConfirm({
      folderIds: Array.from(selection.folders),
      tabIds: Array.from(selection.tabs)
    });
  };

  const hasSelection = selection.folders.size > 0 || selection.tabs.size > 0;

  return (
    <div className="dialog-overlay" role="dialog" aria-modal="true">
      <div className="dialog library-import-dialog">
        <h2>Import Library</h2>
        <p className="library-import-subtitle">
          Choose helpers to add to <strong>{targetName ?? 'this folder'}</strong>. Main/view expressions are hidden automatically.
        </p>
        <div className="library-import-tree">
          {topLevelTabs.length === 0 && topLevelFolders.length === 0 ? (
            <p className="library-import-empty">No reusable expressions detected.</p>
          ) : (
            <ul>
              {topLevelTabs.map((tab) => renderTab(tab))}
              {topLevelFolders.map((folder) => renderFolder(folder))}
            </ul>
          )}
        </div>
        <div className="dialog-footer">
          <button type="button" className="dialog-button" onClick={onCancel}>
            Cancel
          </button>
          <button
            type="button"
            className="dialog-button dialog-button-primary"
            onClick={handleConfirm}
            disabled={!hasSelection}
          >
            Import selected
          </button>
        </div>
      </div>
    </div>
  );
};
