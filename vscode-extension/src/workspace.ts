import * as vscode from 'vscode';

export function getPreferredWorkspaceFolder(): vscode.WorkspaceFolder | undefined {
  const folders = vscode.workspace.workspaceFolders;

  if (!folders || folders.length === 0) {
    return undefined;
  }

  if (folders.length === 1) {
    return folders[0];
  }

  const activeEditorUri = vscode.window.activeTextEditor?.document.uri;
  if (activeEditorUri) {
    const activeFolder = vscode.workspace.getWorkspaceFolder(activeEditorUri);
    if (activeFolder) {
      return activeFolder;
    }
  }

  return folders[0];
}

export async function pickWorkspaceFolder(): Promise<vscode.WorkspaceFolder | undefined> {
  const folders = vscode.workspace.workspaceFolders;

  if (!folders || folders.length === 0) {
    return undefined;
  }

  if (folders.length === 1) {
    return folders[0];
  }

  const preferredFolder = getPreferredWorkspaceFolder();
  const orderedFolders = preferredFolder
    ? [
        preferredFolder,
        ...folders.filter((folder) => folder.uri.toString() !== preferredFolder.uri.toString()),
      ]
    : folders;

  return vscode.window.showQuickPick(
    orderedFolders.map((folder) => ({
      label: folder.name,
      description: folder.uri.fsPath,
      folder,
    })),
    {
      placeHolder: 'Choose a workspace folder to inspect for Unity MCP status',
    },
  ).then((item) => item?.folder);
}
