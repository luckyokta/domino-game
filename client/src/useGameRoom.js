import { useCallback, useEffect, useRef, useState } from 'react';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { api, HUB_URL } from './api';

/**
 * Keeps the player's view of the room up to date.
 *
 * - Fetches the personalised state from the REST API.
 * - Connects to the SignalR hub; the server pushes a content-free "GameUpdated"
 *   message whenever something changes, and we re-fetch our own view.
 * - If the live connection is down, falls back to polling every 3 seconds.
 *
 * Returns { game, connected, fatal, applyState }.
 * `fatal` is set (to a message) when the room is gone or the token is invalid.
 */
export function useGameRoom(code, token) {
  const [game, setGame] = useState(null);
  const [connected, setConnected] = useState(false);
  const [fatal, setFatal] = useState('');
  const aliveRef = useRef(true);

  useEffect(() => {
    aliveRef.current = true;
    return () => {
      aliveRef.current = false;
    };
  }, []);

  const refresh = useCallback(async () => {
    try {
      const state = await api.getState(code, token);
      if (aliveRef.current) setGame(state);
    } catch (err) {
      if (!aliveRef.current) return;
      if (err.status === 404) {
        setFatal('That room no longer exists (the server may have restarted).');
      } else if (err.status === 401) {
        setFatal('You are no longer part of that room.');
      }
      // other errors are transient; the next update or poll will recover
    }
  }, [code, token]);

  // Live connection
  useEffect(() => {
    let cancelled = false;

    /** @type {HubConnection} */
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect({
        // keep retrying forever, backing off up to 15 seconds
        nextRetryDelayInMilliseconds: (ctx) => Math.min(1000 * 2 ** ctx.previousRetryCount, 15000),
      })
      .configureLogging(LogLevel.Warning)
      .build();

    const joinRoom = async () => {
      await connection.invoke('JoinRoom', code, token);
      if (!cancelled) setConnected(true);
    };

    connection.on('GameUpdated', () => {
      refresh();
    });
    connection.onreconnecting(() => {
      if (!cancelled) setConnected(false);
    });
    connection.onreconnected(async () => {
      try {
        await joinRoom();
        await refresh();
      } catch {
        // polling fallback keeps things moving
      }
    });
    connection.onclose(() => {
      if (!cancelled) setConnected(false);
    });

    const startLoop = async () => {
      while (!cancelled) {
        try {
          await connection.start();
          await joinRoom();
          await refresh();
          return;
        } catch {
          if (cancelled) return;
          setConnected(false);
          if (connection.state !== HubConnectionState.Disconnected) {
            try {
              await connection.stop();
            } catch {
              /* ignore */
            }
          }
          await new Promise((resolve) => setTimeout(resolve, 3000));
        }
      }
    };

    refresh();
    startLoop();

    return () => {
      cancelled = true;
      connection.stop();
    };
  }, [code, token, refresh]);

  // Polling fallback while the live connection is down
  useEffect(() => {
    if (connected) return undefined;
    const id = setInterval(refresh, 3000);
    return () => clearInterval(id);
  }, [connected, refresh]);

  return { game, connected, fatal, applyState: setGame };
}
