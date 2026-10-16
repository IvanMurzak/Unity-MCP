// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect } from 'vitest';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import {
  readProjectMarker,
  writeProjectMarker,
  projectMarkerPath,
} from '../src/utils/project-marker.js';

function tmpProject(): string {
  return fs.mkdtempSync(path.join(os.tmpdir(), 'marker-'));
}

describe('project marker', () => {
  it('returns null when the marker is absent', () => {
    const project = tmpProject();
    try {
      expect(readProjectMarker(project)).toBeNull();
    } finally {
      fs.rmSync(project, { recursive: true, force: true });
    }
  });

  it('writes the marker to <project>/.ai-game-dev/project.json and reads it back', () => {
    const project = tmpProject();
    try {
      const written = writeProjectMarker(project, { serverTarget: 'https://ai-game.dev' });
      expect(written).toBe(projectMarkerPath(project));
      expect(written).toContain(path.join('.ai-game-dev', 'project.json'));

      const marker = readProjectMarker(project);
      expect(marker?.serverTarget).toBe('https://ai-game.dev');
    } finally {
      fs.rmSync(project, { recursive: true, force: true });
    }
  });

  it('merges into an existing marker, preserving unrelated keys', () => {
    const project = tmpProject();
    try {
      writeProjectMarker(project, { serverTarget: 'http://localhost:24000', portOverride: 24000 });
      writeProjectMarker(project, { serverTarget: 'https://ai-game.dev' });

      const marker = readProjectMarker(project);
      expect(marker?.serverTarget).toBe('https://ai-game.dev');
      expect(marker?.portOverride).toBe(24000); // preserved
    } finally {
      fs.rmSync(project, { recursive: true, force: true });
    }
  });

  it('is idempotent for the same inputs', () => {
    const project = tmpProject();
    try {
      writeProjectMarker(project, { serverTarget: 'https://ai-game.dev' });
      const first = fs.readFileSync(projectMarkerPath(project), 'utf-8');
      writeProjectMarker(project, { serverTarget: 'https://ai-game.dev' });
      const second = fs.readFileSync(projectMarkerPath(project), 'utf-8');
      expect(second).toBe(first);
    } finally {
      fs.rmSync(project, { recursive: true, force: true });
    }
  });
});
