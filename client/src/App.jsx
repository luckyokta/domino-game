import { useCallback, useState } from 'react';
import Home from './components/Home.jsx';
import Room from './components/Room.jsx';
import { clearSession, loadSession, saveSession } from './storage.js';

export default function App() {
  // { code, token } of the room this browser is playing in (survives page refreshes)
  const [session, setSession] = useState(loadSession);
  const [notice, setNotice] = useState('');

  const handleJoined = useCallback((next) => {
    saveSession(next);
    setSession(next);
    setNotice('');
  }, []);

  const handleLeave = useCallback((message = '') => {
    clearSession();
    setSession(null);
    setNotice(message);
  }, []);

  if (session) {
    return <Room key={session.code} session={session} onLeave={handleLeave} />;
  }

  return <Home onJoined={handleJoined} notice={notice} />;
}
