// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { exec } from 'child_process';
import { verbose } from './ui.js';

/**
 * Open a URL in the user's default browser.
 * Silently ignores errors — the URL is always shown in the terminal as a fallback.
 */
export function openBrowser(url: string): void {
  const platform = process.platform;
  let command: string;

  if (platform === 'darwin') {
    command = `open "${url}"`;
  } else if (platform === 'win32') {
    command = `start "" "${url}"`;
  } else {
    command = `xdg-open "${url}"`;
  }

  verbose(`Opening browser: ${command}`);
  exec(command, (err) => {
    if (err) {
      verbose(`Failed to open browser: ${err.message}`);
    }
  });
}
