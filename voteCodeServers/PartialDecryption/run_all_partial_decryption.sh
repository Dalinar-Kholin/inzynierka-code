#!/bin/bash

# Uruchamia instancje PartialDecryption: dotnet run <serverId>
# Użycie: ./run_all_partial_decryption.sh [totalServers]

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

TOTAL=${1:-10}
LOGDIR="logs_partial_decryption"
pids=()

mkdir -p "$LOGDIR"

for i in $(seq 1 $TOTAL); do
  echo "Startuję PartialDecryption server $i/$TOTAL…"
  nohup dotnet run "$i" > "$LOGDIR/server_$i.log" 2>&1 &
  pids+=("$!")
  echo "Serwer $i PID: ${pids[-1]}"
done

echo "Wystartowano $TOTAL instancji. PIDy: ${pids[*]}"

wait "${pids[@]}"
echo "Wszystkie serwery zakończyły przetwarzanie."
