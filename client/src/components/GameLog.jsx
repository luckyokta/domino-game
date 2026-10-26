export default function GameLog({ entries }) {
  // newest first
  const items = [...entries].reverse();

  return (
    <section className="panel log" aria-label="Game log">
      <h3>Game log</h3>
      {items.length === 0 ? (
        <p className="muted">Nothing has happened yet.</p>
      ) : (
        <ol>
          {items.map((text, i) => (
            <li key={`${entries.length - i}-${text}`}>{text}</li>
          ))}
        </ol>
      )}
    </section>
  );
}
