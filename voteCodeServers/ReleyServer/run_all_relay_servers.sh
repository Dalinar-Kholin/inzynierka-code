set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

TOTAL=${1:-10}
PIDFILE="relay_servers.pids"

# usuń stary plik z PIDami
rm -f "$PIDFILE"

echo "Uruchamiam $TOTAL serwerów ReleyServer w trybie testowym..."

for i in $(seq 1 $TOTAL); do
  PORT=$((5000 + i))
  echo "Startuję ReleyServer $i/$TOTAL na porcie $PORT..."
  nohup python3 server.py "$i" test > /dev/null 2>&1 &
  PID=$!
  echo "$PID" >> "$PIDFILE"
  echo "  Serwer $i PID: $PID (port $PORT)"
  sleep 0.2
done

echo ""
echo "Wystartowano $TOTAL serwerów ReleyServer"
echo "PIDy zapisane w: $PIDFILE"
echo ""
echo "Aby zatrzymać serwery:"
echo "  ./stop_all_relay_servers.sh"
