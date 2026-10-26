import { useState } from 'react';
import { api } from '../api.js';
import { loadName, saveName } from '../storage.js';

// Must match the limits enforced by GamesController.Create on the server.
const MIN_TARGET = 10;
const MAX_TARGET = 1000;

export default function Home({ onJoined, notice }) {
  const [name, setName] = useState(loadName);
  const [target, setTarget] = useState('100'); // kept as text so the field can be edited freely
  const [roomCode, setRoomCode] = useState(() => {
    const fromLink = new URLSearchParams(window.location.search).get('room');
    return fromLink ? fromLink.toUpperCase() : '';
  });
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  async function run(action) {
    setBusy(true);
    setError('');
    try {
      const result = await action();
      saveName(name.trim());
      // drop ?room=... from the address bar
      window.history.replaceState({}, '', window.location.pathname);
      onJoined({ code: result.code, token: result.token });
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  }

  const targetValue = /^\d+$/.test(target.trim()) ? Number(target.trim()) : NaN;
  const targetValid = targetValue >= MIN_TARGET && targetValue <= MAX_TARGET;

  const create = (e) => {
    e.preventDefault();
    if (!targetValid) return;
    run(() => api.createGame(name.trim(), targetValue));
  };

  const join = (e) => {
    e.preventDefault();
    run(() => api.joinGame(roomCode.trim(), name.trim()));
  };

  return (
    <main className="center-page">
      <header className="hero">
        <h1>Domino</h1>
        <p>Double-six dominoes with friends — 2 to 4 players, each on their own screen.</p>
      </header>

      {notice && (
        <p className="banner" role="alert">
          {notice}
        </p>
      )}

      <section className="panel home-card">
        <label className="field">
          <span>Your name</span>
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            maxLength={20}
            placeholder="e.g. Lucky"
            autoComplete="nickname"
          />
        </label>

        <div className="home-columns">
          <form onSubmit={create} className="home-col">
            <h2>Create a game</h2>
            <label className="field">
              <span>Play to</span>
              <input
                type="number"
                inputMode="numeric"
                min={MIN_TARGET}
                max={MAX_TARGET}
                step="1"
                value={target}
                onChange={(e) => setTarget(e.target.value)}
                aria-invalid={!targetValid}
                aria-describedby="target-hint"
              />
            </label>
            <p id="target-hint" className={targetValid ? 'muted small hint' : 'error small hint'}>
              Points needed to win the match ({MIN_TARGET}–{MAX_TARGET}).
            </p>
            <button type="submit" className="btn big" disabled={busy || !name.trim() || !targetValid}>
              Create room
            </button>
          </form>

          <form onSubmit={join} className="home-col">
            <h2>Join a game</h2>
            <label className="field">
              <span>Room code</span>
              <input
                value={roomCode}
                onChange={(e) => setRoomCode(e.target.value.toUpperCase())}
                maxLength={5}
                placeholder="ABCDE"
                className="code-input"
                autoCapitalize="characters"
              />
            </label>
            <button type="submit" className="btn big" disabled={busy || !name.trim() || roomCode.trim().length < 5}>
              Join room
            </button>
          </form>
        </div>

        {error && (
          <p className="error" role="alert">
            {error}
          </p>
        )}
      </section>
    </main>
  );
}
