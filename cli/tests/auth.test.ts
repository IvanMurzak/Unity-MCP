// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { deviceAuthFlow, type DeviceAuthCallbacks } from '../src/utils/auth.js';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makeCallbacks(): DeviceAuthCallbacks & {
  userCode: string | null;
  verificationUrl: string | null;
  pollingStarted: boolean;
} {
  const cbs = {
    userCode: null as string | null,
    verificationUrl: null as string | null,
    pollingStarted: false,
    onUserCode: (code: string, url: string) => {
      cbs.userCode = code;
      cbs.verificationUrl = url;
    },
    onPolling: () => {
      cbs.pollingStarted = true;
    },
  };
  return cbs;
}

const AUTHORIZE_RESPONSE = {
  device_code: 'test-device-code',
  user_code: 'ABCD-1234',
  verification_uri: 'https://example.com/device',
  verification_uri_complete: 'https://example.com/device?code=ABCD-1234',
  expires_in: 300,
  interval: 0, // will be clamped to 5s; we override via vi.useFakeTimers
};

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('deviceAuthFlow', () => {
  const originalFetch = globalThis.fetch;

  afterEach(() => {
    globalThis.fetch = originalFetch;
    vi.restoreAllMocks();
  });

  it('returns success when token is received on first poll', async () => {
    const fetchMock = vi.fn()
      // authorize call
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(AUTHORIZE_RESPONSE),
      })
      // first poll → success
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ access_token: 'my-token', token_type: 'bearer' }),
      });
    globalThis.fetch = fetchMock;

    const callbacks = makeCallbacks();
    const result = await deviceAuthFlow('https://example.com', 'test-client', callbacks, 10);

    expect(result).toEqual({ success: true, accessToken: 'my-token' });
    expect(callbacks.userCode).toBe('ABCD-1234');
    expect(callbacks.pollingStarted).toBe(true);
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('polls through authorization_pending then succeeds', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(AUTHORIZE_RESPONSE),
      })
      // first poll → pending
      .mockResolvedValueOnce({
        ok: false,
        json: () => Promise.resolve({ error: 'authorization_pending' }),
      })
      // second poll → success
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ access_token: 'delayed-token', token_type: 'bearer' }),
      });
    globalThis.fetch = fetchMock;

    const result = await deviceAuthFlow('https://example.com', 'test', makeCallbacks(), 10);

    expect(result).toEqual({ success: true, accessToken: 'delayed-token' });
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });

  it('returns denied when user denies authorization', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(AUTHORIZE_RESPONSE),
      })
      .mockResolvedValueOnce({
        ok: false,
        json: () => Promise.resolve({ error: 'access_denied', error_description: 'User denied' }),
      });
    globalThis.fetch = fetchMock;

    const result = await deviceAuthFlow('https://example.com', 'test', makeCallbacks(), 10);

    expect(result).toEqual({ success: false, reason: 'denied', message: 'User denied' });
  });

  it('returns expired when device code expires', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(AUTHORIZE_RESPONSE),
      })
      .mockResolvedValueOnce({
        ok: false,
        json: () => Promise.resolve({ error: 'expired_token', error_description: 'Code expired' }),
      });
    globalThis.fetch = fetchMock;

    const result = await deviceAuthFlow('https://example.com', 'test', makeCallbacks(), 10);

    expect(result).toEqual({ success: false, reason: 'expired', message: 'Code expired' });
  });

  it('returns error when authorize endpoint fails', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({
        ok: false,
        status: 500,
        text: () => Promise.resolve('Internal Server Error'),
      });
    globalThis.fetch = fetchMock;

    const result = await deviceAuthFlow('https://example.com', 'test', makeCallbacks(), 10);

    expect(result.success).toBe(false);
    expect(result.success === false && result.reason).toBe('error');
  });

  it('sends correct grant_type in token poll', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(AUTHORIZE_RESPONSE),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ access_token: 'tok', token_type: 'bearer' }),
      });
    globalThis.fetch = fetchMock;

    await deviceAuthFlow('https://example.com', 'test', makeCallbacks(), 10);

    const tokenCall = fetchMock.mock.calls[1];
    const body = JSON.parse(tokenCall[1].body);
    expect(body.grant_type).toBe('urn:ietf:params:oauth:grant-type:device_code');
    expect(body.device_code).toBe('test-device-code');
  });

  it('sends client_label in authorize request', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve(AUTHORIZE_RESPONSE),
      })
      .mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({ access_token: 'tok', token_type: 'bearer' }),
      });
    globalThis.fetch = fetchMock;

    await deviceAuthFlow('https://example.com', 'My CLI', makeCallbacks(), 10);

    const authCall = fetchMock.mock.calls[0];
    const body = JSON.parse(authCall[1].body);
    expect(body.client_label).toBe('My CLI');
  });
});
