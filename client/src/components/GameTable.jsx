import { useEffect, useState } from 'react';
import Board from './Board.jsx';
import GameLog from './GameLog.jsx';
import PlayerCards from './PlayerCards.jsx';
import RoundSummary from './RoundSummary.jsx';
import Tile from './Tile.jsx';

function statusText(game) {
  if (game.isYourTurn) {
    if (game.canDraw) return 'You have no playable tile. Draw from the boneyard.';
    if (game.canPass) return 'No playable tile and the boneyard is empty. You must pass.';
    if (game.board.length === 0) return 'You start! Play any tile.';
    return 'Your turn. Choose a tile to play.';
  }
  const current = game.players.find((p) => p.seat === game.currentSeat);
  return `Waiting for ${current ? current.name : 'the next player'}…`;
}

export default function GameTable({ game, actions, busy }) {
  // index of a hand tile that fits on both ends and is waiting for an end to be chosen
  const [selected, setSelected] = useState(null);

  useEffect(() => {
    setSelected(null);
  }, [game.isYourTurn, game.board.length, game.hand.length]);

  const playing = game.phase === 'playing';
  const canAct = playing && game.isYourTurn && !busy;
  const selectedTile = selected != null ? game.hand[selected] : null;

  function playTile(tile, end) {
    setSelected(null);
    actions.play(tile.left, tile.right, end);
  }

  function onTileClick(index) {
    if (!canAct) return;

    const tile = game.hand[index];
    const { canPlayLeft, canPlayRight } = tile;
    if (!canPlayLeft && !canPlayRight) return;

    const needsChoice =
      canPlayLeft && canPlayRight && game.board.length > 0 && game.leftEnd !== game.rightEnd;

    if (needsChoice) {
      setSelected(index);
    } else {
      playTile(tile, canPlayLeft ? 'left' : 'right');
    }
  }

  return (
    <main className="table-layout">
      <div className="table-main">
        <PlayerCards players={game.players} targetScore={game.targetScore} />

        <Board tiles={game.board} leftEnd={game.leftEnd} rightEnd={game.rightEnd} />

        {playing ? (
          <section className={game.isYourTurn ? 'panel actions your-turn' : 'panel actions'}>
            <div className="actions-top">
              <p className="status" role="status">
                {statusText(game)}
              </p>
              <div className="actions-buttons">
                <span className="boneyard" title="Tiles left in the boneyard">
                  🂠 Boneyard: <b>{game.boneyardCount}</b>
                </span>
                <button
                  type="button"
                  className="btn"
                  disabled={!game.canDraw || busy}
                  onClick={actions.draw}
                >
                  Draw tile
                </button>
                <button
                  type="button"
                  className="btn ghost"
                  disabled={!game.canPass || busy}
                  onClick={actions.pass}
                >
                  Pass
                </button>
              </div>
            </div>

            {selectedTile && (
              <div className="end-choice" role="group" aria-label="Choose an end">
                <span>
                  Where do you want to play{' '}
                  <b>
                    [{selectedTile.left}|{selectedTile.right}]
                  </b>
                  ?
                </span>
                <button type="button" className="btn" disabled={busy} onClick={() => playTile(selectedTile, 'left')}>
                  ◀ Left end ({game.leftEnd})
                </button>
                <button type="button" className="btn" disabled={busy} onClick={() => playTile(selectedTile, 'right')}>
                  Right end ({game.rightEnd}) ▶
                </button>
                <button type="button" className="btn ghost" onClick={() => setSelected(null)}>
                  Cancel
                </button>
              </div>
            )}

            <div className="hand" aria-label="Your hand">
              {game.hand.map((t, i) => {
                const playable = game.isYourTurn && (t.canPlayLeft || t.canPlayRight);
                return (
                  <Tile
                    key={`${t.left}-${t.right}`}
                    left={t.left}
                    right={t.right}
                    size="lg"
                    playable={playable}
                    dim={game.isYourTurn && !playable}
                    selected={selected === i}
                    disabled={!canAct || !playable}
                    onClick={() => onTileClick(i)}
                  />
                );
              })}
              {game.hand.length === 0 && <p className="muted">You have no tiles left.</p>}
            </div>
          </section>
        ) : (
          <RoundSummary game={game} actions={actions} busy={busy} />
        )}
      </div>

      <aside className="table-side">
        <GameLog entries={game.log} />
      </aside>
    </main>
  );
}
