import { useState } from 'react';

const MAX_PLAYERS = 4;

export default function Lobby({ game, actions, busy }) {
  const [copied, setCopied] = useState(false);

  const link = `${window.location.origin}/?room=${game.code}`;
  const me = game.players.find((p) => p.isYou);
  const isHost = Boolean(me?.isHost);

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(link);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch {
      window.prompt('Copy this link and send it to your friends:', link);
    }
  }

  const slots = Array.from({ length: MAX_PLAYERS }, (_, i) => game.players[i] ?? null);

  return (
    <main className="center-page">
      <section className="panel lobby">
        <p className="eyebrow">Room code</p>
        <div className="room-code" aria-label={`Room code ${game.code}`}>
          {game.code}
        </div>

        <button type="button" className="btn ghost" onClick={copyLink}>
          {copied ? '✓ Link copied' : 'Copy invite link'}
        </button>

        <ul className="slots">
          {slots.map((p, i) => (
            <li key={i} className={p ? 'slot filled' : 'slot'}>
              {p ? (
                <>
                  <span className="slot-name">
                    {p.isHost && (
                      <span className="crown" title="Host">
                        ♛{' '}
                      </span>
                    )}
                    {p.name}
                  </span>
                  {p.isYou && <span className="tag">you</span>}
                </>
              ) : (
                <span className="muted">Waiting for player…</span>
              )}
            </li>
          ))}
        </ul>

        <p className="muted small">
          First to <b>{game.targetScore}</b> points wins the match. 2–4 players.
        </p>

        {isHost ? (
          <button
            type="button"
            className="btn big"
            disabled={busy || game.players.length < 2}
            onClick={actions.start}
          >
            {game.players.length < 2 ? 'Need at least 2 players' : 'Start game'}
          </button>
        ) : (
          <p className="status">Waiting for the host to start the game…</p>
        )}
      </section>
    </main>
  );
}
