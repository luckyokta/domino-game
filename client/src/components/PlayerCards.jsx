import Tile from './Tile.jsx';

export default function PlayerCards({ players, targetScore }) {
  return (
    <div className="pcards">
      {players.map((p) => {
        const pct = Math.min(100, Math.round((p.score / targetScore) * 100));
        const classes = ['pcard'];
        if (p.isCurrent) classes.push('current');
        if (p.isYou) classes.push('you');

        return (
          <div key={p.seat} className={classes.join(' ')}>
            <div className="pname">
              {p.isHost && (
                <span className="crown" title="Host">
                  ♛
                </span>
              )}
              <span className="pname-text">{p.name}</span>
              {p.isYou && <span className="tag">you</span>}
              {p.isCurrent && <span className="tag turn">turn</span>}
            </div>

            <div className="pscore">
              <strong>{p.score}</strong>
              <span>/ {targetScore}</span>
            </div>
            <div className="pbar" aria-hidden="true">
              <i style={{ width: `${pct}%` }} />
            </div>

            <div className="ptiles">
              <span className="ptiles-stack">
                {Array.from({ length: Math.min(p.tileCount, 14) }, (_, i) => (
                  <Tile key={i} faceDown size="xs" vertical />
                ))}
              </span>
              <span className="ptiles-count">
                {p.tileCount} {p.tileCount === 1 ? 'tile' : 'tiles'}
              </span>
            </div>
          </div>
        );
      })}
    </div>
  );
}
