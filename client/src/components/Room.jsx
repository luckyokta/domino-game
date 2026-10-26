import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { api } from '../api.js';
import { useGameRoom } from '../useGameRoom.js';
import GameTable from './GameTable.jsx';
import Lobby from './Lobby.jsx';

function TopBar({ game, connected, onLeave }) {
  const [copied, setCopied] = useState(false);

  async function copyCode() {
    try {
      await navigator.clipboard.writeText(game.code);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      /* ignore */
    }
  }

  return (
    <header className="topbar">
      <div className="brand">Domino</div>

      <div className="topbar-info">
        <button type="button" className="chip" onClick={copyCode} title="Copy room code">
          Room <b>{game.code}</b> {copied ? '✓' : ''}
        </button>
        {game.phase !== 'lobby' && (
          <span className="chip static">
            Round <b>{game.round}</b> · to <b>{game.targetScore}</b>
          </span>
        )}
        <span className={connected ? 'chip static live' : 'chip static offline'}>
          <i className="dot" /> {connected ? 'Live' : 'Reconnecting…'}
        </span>
      </div>

      <button type="button" className="btn ghost small" onClick={onLeave}>
        Leave
      </button>
    </header>
  );
}

export default function Room({ session, onLeave }) {
  const { code, token } = session;
  const { game, connected, fatal, applyState } = useGameRoom(code, token);
  const [busy, setBusy] = useState(false);
  const [toast, setToast] = useState('');
  const toastTimer = useRef(null);

  // Room vanished or token rejected -> back to the home screen
  useEffect(() => {
    if (fatal) onLeave(fatal);
  }, [fatal, onLeave]);

  useEffect(() => () => clearTimeout(toastTimer.current), []);

  const showToast = useCallback((message) => {
    setToast(message);
    clearTimeout(toastTimer.current);
    toastTimer.current = setTimeout(() => setToast(''), 4000);
  }, []);

  const act = useCallback(
    async (call) => {
      setBusy(true);
      try {
        const state = await call();
        if (state) applyState(state);
      } catch (err) {
        if (err.status === 404 || err.status === 401) {
          onLeave(err.message);
        } else {
          showToast(err.message);
        }
      } finally {
        setBusy(false);
      }
    },
    [applyState, onLeave, showToast]
  );

  const actions = useMemo(
    () => ({
      start: () => act(() => api.start(code, token)),
      play: (left, right, end) => act(() => api.play(code, token, left, right, end)),
      draw: () => act(() => api.draw(code, token)),
      pass: () => act(() => api.pass(code, token)),
      nextRound: () => act(() => api.nextRound(code, token)),
    }),
    [act, code, token]
  );

  function leave() {
    if (window.confirm('Leave this room? You will not be able to rejoin your seat.')) {
      onLeave('');
    }
  }

  if (!game) {
    return (
      <main className="center-page">
        <p className="status">Connecting to room {code}…</p>
      </main>
    );
  }

  return (
    <>
      <TopBar game={game} connected={connected} onLeave={leave} />

      {game.phase === 'lobby' ? (
        <Lobby game={game} actions={actions} busy={busy} />
      ) : (
        <GameTable game={game} actions={actions} busy={busy} />
      )}

      {toast && (
        <div className="toast" role="alert">
          {toast}
        </div>
      )}
    </>
  );
}
