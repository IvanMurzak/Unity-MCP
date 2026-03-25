// Copyright (c) 2024 Ivan Murzak. All rights reserved.
// Licensed under the Apache License, Version 2.0.

import { verbose } from './ui.js';

// ─── Types ───────────────────────────────────────────────────────────────────

export interface DeviceAuthorizeResponse {
  device_code: string;
  user_code: string;
  verification_uri: string;
  verification_uri_complete: string;
  expires_in: number;
  interval: number;
}

interface DeviceTokenSuccessResponse {
  access_token: string;
  token_type: string;
}

interface DeviceTokenErrorResponse {
  error: string;
  error_description?: string;
}

export type DeviceAuthResult =
  | { success: true; accessToken: string }
  | { success: false; reason: 'expired' | 'denied' | 'error'; message: string };

export interface DeviceAuthCallbacks {
  onUserCode: (userCode: string, verificationUrl: string) => void;
  onPolling?: () => void;
}

// ─── Flow ────────────────────────────────────────────────────────────────────

/**
 * Run the RFC 8628 Device Authorization Grant flow against the given server.
 *
 * 1. POST /api/auth/device/authorize  → get device_code + user_code
 * 2. Invoke onUserCode so the caller can display instructions / open browser
 * 3. Poll /api/auth/device/token until success, denial, or expiry
 */
export async function deviceAuthFlow(
  baseUrl: string,
  clientLabel: string,
  callbacks: DeviceAuthCallbacks,
  minIntervalMs?: number,
): Promise<DeviceAuthResult> {
  const authorizeUrl = `${baseUrl}/api/auth/device/authorize`;
  verbose(`POST ${authorizeUrl}`);

  const initResponse = await fetchWithTimeout(
    authorizeUrl,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ client_label: clientLabel }),
    },
    30_000,
  );

  if (!initResponse.ok) {
    const text = await initResponse.text();
    return {
      success: false,
      reason: 'error',
      message: `Failed to initiate device auth (HTTP ${initResponse.status}): ${text}`,
    };
  }

  const auth = (await initResponse.json()) as DeviceAuthorizeResponse;
  verbose(`Device code received, user code: ${auth.user_code}, expires in ${auth.expires_in}s`);

  callbacks.onUserCode(auth.user_code, auth.verification_uri_complete);
  callbacks.onPolling?.();

  const tokenUrl = `${baseUrl}/api/auth/device/token`;
  const deadline = Date.now() + auth.expires_in * 1000;
  const effectiveMinInterval = minIntervalMs ?? 5000;
  let interval = Math.max(auth.interval * 1000, effectiveMinInterval);

  while (Date.now() < deadline) {
    await sleep(interval);

    if (Date.now() >= deadline) break;

    verbose(`Polling ${tokenUrl} (interval=${interval / 1000}s)`);

    const pollResponse = await fetchWithTimeout(
      tokenUrl,
      {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          device_code: auth.device_code,
          grant_type: 'urn:ietf:params:oauth:grant-type:device_code',
        }),
      },
      15_000,
    );

    if (pollResponse.ok) {
      const data = (await pollResponse.json()) as DeviceTokenSuccessResponse;
      return { success: true, accessToken: data.access_token };
    }

    let errorData: DeviceTokenErrorResponse;
    try {
      errorData = (await pollResponse.json()) as DeviceTokenErrorResponse;
    } catch {
      return {
        success: false,
        reason: 'error',
        message: `Unexpected response from token endpoint (HTTP ${pollResponse.status})`,
      };
    }
    verbose(`Poll response: ${errorData.error} — ${errorData.error_description ?? ''}`);

    switch (errorData.error) {
      case 'authorization_pending':
        break;

      case 'slow_down':
        interval = Math.min(interval + 5000, 30000);
        verbose(`Slowing down, new interval: ${interval / 1000}s`);
        break;

      case 'expired_token':
        return {
          success: false,
          reason: 'expired',
          message: errorData.error_description ?? 'Device code expired. Please try again.',
        };

      case 'access_denied':
        return {
          success: false,
          reason: 'denied',
          message: errorData.error_description ?? 'Authorization was denied.',
        };

      default:
        return {
          success: false,
          reason: 'error',
          message: errorData.error_description ?? `Unexpected error: ${errorData.error}`,
        };
    }
  }

  return {
    success: false,
    reason: 'expired',
    message: 'Device code expired. Please try again.',
  };
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function fetchWithTimeout(
  url: string,
  options: RequestInit,
  timeoutMs: number,
): Promise<Response> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), timeoutMs);
  try {
    return await fetch(url, { ...options, signal: controller.signal });
  } finally {
    clearTimeout(timer);
  }
}
