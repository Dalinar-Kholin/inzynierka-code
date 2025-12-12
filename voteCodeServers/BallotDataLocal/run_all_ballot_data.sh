#!/bin/bash

# Uruchamia 10 instancji BallotDataLocal: dotnet run <serverId>
# Wszystkie startują równolegle w tle. stdout/err -> /dev/null (brak logów).
# Użycie: ./run_all_ballot_data.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

TOTAL=10
pids=()

for i in $(seq 1 $TOTAL); do
  echo "Startuję BallotDataLocal $i/$TOTAL…"
  nohup dotnet run "$i" > /dev/null 2>&1 &
  pids+=("$!")
  echo "Serwer $i PID: ${pids[-1]}"
 done

 echo "Wystartowano $TOTAL instancji. PIDy: ${pids[*]}"

wait "${pids[@]}"
echo "done"
