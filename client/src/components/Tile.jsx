// Which of the 9 cells of a 3x3 grid hold a pip, for each face value.
//   0 1 2
//   3 4 5
//   6 7 8
const PIP_CELLS = {
  0: [],
  1: [4],
  2: [0, 8],
  3: [0, 4, 8],
  4: [0, 2, 6, 8],
  5: [0, 2, 4, 6, 8],
  6: [0, 2, 3, 5, 6, 8],
};

function Half({ value }) {
  const cells = PIP_CELLS[value] ?? [];
  return (
    <span className="half" aria-hidden="true">
      {Array.from({ length: 9 }, (_, i) => (
        <span key={i} className={cells.includes(i) ? 'pip on' : 'pip'} />
      ))}
    </span>
  );
}

/**
 * A domino. Pass onClick to render it as a button.
 * size: 'xs' | 'sm' | 'md' | 'lg'
 */
export default function Tile({
  left,
  right,
  vertical = false,
  size = 'md',
  faceDown = false,
  playable = false,
  dim = false,
  selected = false,
  edge = false,
  disabled = false,
  onClick,
}) {
  const classes = ['tile', `tile-${size}`, vertical ? 'v' : 'h'];
  if (faceDown) classes.push('back');
  if (playable) classes.push('playable');
  if (dim) classes.push('dim');
  if (selected) classes.push('selected');
  if (edge) classes.push('edge');
  const className = classes.join(' ');

  if (faceDown) {
    return <span className={className} aria-hidden="true" />;
  }

  const label = `Domino ${left} and ${right}`;
  const content = (
    <>
      <Half value={left} />
      <Half value={right} />
    </>
  );

  if (onClick) {
    return (
      <button type="button" className={className} onClick={onClick} disabled={disabled} aria-label={label}>
        {content}
      </button>
    );
  }

  return (
    <span className={className} role="img" aria-label={label}>
      {content}
    </span>
  );
}
