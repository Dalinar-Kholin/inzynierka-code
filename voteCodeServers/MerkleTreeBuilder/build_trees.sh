#!/bin/bash

# Buduje merkle trees dla CommC0, CommC1, CommB dla wszystkich serwerów
# Użycie: ./build_trees.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "Building Merkle Trees for all servers..."

for i in {1..10}
do
    echo "=== Building Merkle Tree for Server $i ==="
    dotnet run $i
    echo ""
done

echo "All Merkle Trees built successfully!"
