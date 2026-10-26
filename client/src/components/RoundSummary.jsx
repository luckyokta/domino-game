import Tile from './Tile.jsx';

const pipTotal = (tiles) => tiles.reduce((sum, t) => sum + t.left + t.right, 0);

/** Shown in place of the hand once a round (or the whole match) has finished. */
export default function RoundSummary({ game, actions, busy }) {
  const bySeat = (seat) => game.players.find((p) => p.seat === seat);

  const matchOver = game.phase === 'matchOver';
  // roundWinner is null when a blocked round ended in a tie (nobody scores)
  const roundWinner = game.roundWinner != null ? bySeat(game.roundWinner) : null;
  const matchWinner = matchOver && game.matchWinner != null ? bySeat(game.matchWinner) : null;
  const blocked = !game.players.some((p) => p.tileCount === 0);

  return (
    <section className="panel summary" aria-live="polite">
      <header>
        <h2>{matchWinner ? `🏆 ${matchWinner.name} wins the match!` : `Round ${game.round} is over`}</h2>
        <p>
          {blocked ? 'The game was blocked. ' : ''}
          {roundWinner ? (
            <>
              <b>{roundWinner.name}</b> scores <b>{game.lastRoundPoints}</b>{' '}
              {game.lastRoundPoints === 1 ? 'point' : 'points'}.
            </>
          ) : (
            "It's a tie, so nobody scores."
          )}
        </p>
      </header>

      <div className="summary-hands">
        {game.players.map((p) => {
          const isWinner = game.roundWinner === p.seat;
          const tiles = p.revealedHand ?? [];

          return (
            <div key={p.seat} className={isWinner ? 'summary-row winner' : 'summary-row'}>
              <div className="summary-name">
                {isWinner && <span aria-hidden="true">★ </span>}
                {p.name}
                {p.isYou && <span className="tag">you</span>}
              </div>
              <div className="summary-tiles">
                {tiles.length === 0 ? (
                  <span className="muted">no tiles left</span>
                ) : (
                  tiles.map((t) => <Tile key={`${t.left}-${t.right}`} left={t.left} right={t.right} size="sm" />)
                )}
              </div>
              <div className="summary-pips">{pipTotal(tiles)} pips</div>
              <div className="summary-score">
                <strong>{p.score}</strong> pts
              </div>
            </div>
          );
        })}
      </div>

      <footer>
        {matchOver ? (
          game.canStart ? (
            <button type="button" className="btn" disabled={busy} onClick={actions.start}>
              Play again
            </button>
          ) : (
            <p className="muted">Waiting for the host to start a new match…</p>
          )
        ) : (
          <button type="button" className="btn" disabled={busy} onClick={actions.nextRound}>
            Start next round
          </button>
        )}
      </footer>
    </section>
  );
}
