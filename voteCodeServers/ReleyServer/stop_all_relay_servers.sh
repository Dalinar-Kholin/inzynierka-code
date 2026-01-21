SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

PIDFILE="relay_servers.pids"

echo "Zatrzymuję serwery ReleyServer..."

if [ -f "$PIDFILE" ]; then
  while read -r PID; do
    if [ -n "$PID" ] && ps -p "$PID" > /dev/null 2>&1; then
      echo "  Zatrzymuję PID: $PID"
      kill "$PID" 2>/dev/null || true
    fi
  done < "$PIDFILE"
  sleep 0.5
fi

# znajdź i zabij wszystkie procesy server.py
echo "  Szukam pozostałych procesów server.py..."
PIDS=$(pgrep -f "python.*server.py" || true)

if [ -n "$PIDS" ]; then
  echo "  Znaleziono procesy: $PIDS"
  echo "$PIDS" | xargs -r kill 2>/dev/null || true
  sleep 0.5
  
  # wymuś zamknięcie jeśli jeszcze działają
  PIDS=$(pgrep -f "python.*server.py" || true)
  if [ -n "$PIDS" ]; then
    echo "  Wymuszam zamknięcie: $PIDS"
    echo "$PIDS" | xargs -r kill -9 2>/dev/null || true
  fi
fi

rm -f "$PIDFILE"

echo ""
echo "Wszystkie serwery zatrzymane"
