// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { execFile } from 'child_process';
import { verbose } from './ui.js';

/**
 * Open a URL in the user's default browser.
 * Silently ignores errors — the URL is always shown in the terminal as a fallback.
 */
export function openBrowser(url: string): void {
  const platform = process.platform;
  let cmd: string;
  let args: string[];

  if (platform === 'darwin') {
    cmd = 'open';
    args = [url];
  } else if (platform === 'win32') {
    cmd = 'cmd';
    args = ['/c', 'start', '', url];
  } else {
    cmd = 'xdg-open';
    args = [url];
  }

  verbose(`Opening browser: ${cmd} ${args.join(' ')}`);
  execFile(cmd, args, (err) => {
    if (err) {
      verbose(`Failed to open browser: ${err.message}`);
    }
  });
}
