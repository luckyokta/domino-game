// Leave VITE_API_URL empty in development (the Vite proxy handles /api and /hubs).
// Set it (e.g. https://my-api.example.com) when the API is hosted on a different origin.
const BASE = (import.meta.env.VITE_API_URL || '').replace(/\/$/, '');

export const HUB_URL = `${BASE}/hubs/game`;

async function request(method, path, { token, body } = {}) {
  const headers = {};
  if (token) headers['X-Player-Token'] = token;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  let response;
  try {
    response = await fetch(`${BASE}${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch {
    const error = new Error('Cannot reach the server. Is the API running?');
    error.status = 0;
    throw error;
  }

  let data = null;
  try {
    data = await response.json();
  } catch {
    // empty or non-JSON body
  }

  if (!response.ok) {
    const error = new Error(data?.error || `Request failed (${response.status}).`);
    error.status = response.status;
    throw error;
  }

  return data;
}

const room = (code) => `/api/games/${encodeURIComponent(code)}`;

export const api = {
  createGame: (playerName, targetScore) =>
    request('POST', '/api/games', { body: { playerName, targetScore } }),

  joinGame: (code, playerName) =>
    request('POST', `${room(code)}/join`, { body: { playerName } }),

  getState: (code, token) => request('GET', room(code), { token }),

  start: (code, token) => request('POST', `${room(code)}/start`, { token }),

  play: (code, token, left, right, end) =>
    request('POST', `${room(code)}/play`, { token, body: { left, right, end } }),

  draw: (code, token) => request('POST', `${room(code)}/draw`, { token }),

  pass: (code, token) => request('POST', `${room(code)}/pass`, { token }),

  nextRound: (code, token) => request('POST', `${room(code)}/next-round`, { token }),
};
