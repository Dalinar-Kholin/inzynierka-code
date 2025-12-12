#!/bin/bash

# Uruchamia 10 instancji DataInitLocal: dotnet run <serverId>
# Wszystkie startują równolegle w tle. Brak zapisu logów (stdout/err -> /dev/null)
# Użycie: ./run_all_data_init.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

TOTAL=10
LOGDIR="logs_data_init"
pids=()

for i in $(seq 1 $TOTAL); do
  echo "Startuję DataInitLocal $i/$TOTAL…"
  nohup dotnet run "$i" > /dev/null 2>&1 &
  pids+=("$!")
  echo "Serwer $i PID: ${pids[-1]}"
done

echo "Wystartowano $TOTAL instancji. PIDy: ${pids[*]}"

wait "${pids[@]}"
echo "done"