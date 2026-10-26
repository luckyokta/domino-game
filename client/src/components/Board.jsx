import { useLayoutEffect, useMemo, useRef, useState } from 'react';
import Tile from './Tile.jsx';

const GAP = 3; // space between tiles in a row, px
const EPS = 0.5; // sub-pixel tolerance when testing whether a tile fits

// Width of one tile as drawn by Tile.jsx / styles.css: a tile lying down is two
// halves (s each) plus borders; a double is drawn upright so it is only one half wide.
const tileWidth = (t, s) => (t.left === t.right ? s + 2 : 2 * s + 2);

/**
 * Splits the chain into snake rows using each tile's REAL width.
 *
 * Row 1 runs left -> right, row 2 right -> left, row 3 left -> right, ...
 * Each row starts exactly where the previous one stopped (x), so the first tile of
 * a row sits directly under the last tile of the row above and the chain turns
 * like a U, with no gaps from narrow (double) tiles.
 *
 * Returns [{ reversed, offset, items: [{ tile, index }] }] where `offset` is the x
 * position (px from the left of the board) where the row starts.
 */
export function packRows(tiles, width, s) {
  if (!width || tiles.length === 0) {
    return [{ reversed: false, offset: 0, items: tiles.map((tile, index) => ({ tile, index })) }];
  }

  // Everything fits on one line: just centre it.
  const total = tiles.reduce((sum, t, i) => sum + tileWidth(t, s) + (i > 0 ? GAP : 0), 0);
  if (total <= width) {
    return [{ reversed: false, offset: (width - total) / 2, items: tiles.map((tile, index) => ({ tile, index })) }];
  }

  const rows = [];
  let index = 0;
  let reversed = false;
  let x = 0;

  while (index < tiles.length) {
    const available = reversed ? x : width - x; // room between the start point and the wall
    const items = [];
    let used = 0;

    while (index < tiles.length) {
      const w = tileWidth(tiles[index], s);
      const next = used + (items.length > 0 ? GAP : 0) + w;
      if (items.length > 0 && next > available + EPS) break; // always place at least one tile
      items.push({ tile: tiles[index], index });
      used = next;
      index += 1;
    }

    rows.push({ reversed, offset: x, items });

    x = reversed ? x - used : x + used;
    x = Math.min(Math.max(x, 0), width); // stay inside the board
    reversed = !reversed;
  }

  return rows;
}

export default function Board({ tiles, leftEnd, rightEnd }) {
  const boardRef = useRef(null);
  const [size, setSize] = useState({ width: 0, s: 40 });

  useLayoutEffect(() => {
    const el = boardRef.current;
    if (!el) return undefined;

    const measure = () => {
      const style = getComputedStyle(el);
      const s = parseFloat(style.getPropertyValue('--board-s')) || 40;
      const width = el.clientWidth - parseFloat(style.paddingLeft) - parseFloat(style.paddingRight);
      setSize((prev) => (prev.width === width && prev.s === s ? prev : { width, s }));
    };

    measure();
    const observer = new ResizeObserver(measure);
    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  const rows = useMemo(() => packRows(tiles, size.width, size.s), [tiles, size]);

  return (
    <section className="board-wrap" aria-label="Board">
      <div className="board-ends">
        <span>
          ◀ Left end <b>{leftEnd ?? '–'}</b>
        </span>
        <span>
          Right end <b>{rightEnd ?? '–'}</b> ▶
        </span>
      </div>

      <div className="board" ref={boardRef}>
        {tiles.length === 0 && <p className="board-empty">No tiles on the table yet.</p>}

        {rows.map((row, r) => (
          <div
            key={r}
            className={row.reversed ? 'board-row rev' : 'board-row'}
            style={{
              gap: GAP,
              // left -> right rows start `offset` px from the left wall;
              // right -> left rows start `offset` px from the left wall too, i.e. (width - offset) from the right wall.
              ...(row.reversed
                ? { paddingRight: Math.max(0, size.width - row.offset) }
                : { paddingLeft: row.offset }),
            }}
          >
            {row.items.map(({ tile, index }) => (
              <Tile
                key={`${index}-${tile.left}-${tile.right}`}
                // In right -> left rows the tile is drawn mirrored so matching pips stay side by side.
                left={row.reversed ? tile.right : tile.left}
                right={row.reversed ? tile.left : tile.right}
                size="md"
                vertical={tile.left === tile.right}
                edge={index === 0 || index === tiles.length - 1}
              />
            ))}
          </div>
        ))}
      </div>
    </section>
  );
}
