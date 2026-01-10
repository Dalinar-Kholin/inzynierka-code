# Merkle Tree Builder

Buduje drzewa Merkle z commitmentów (CommC0, CommC1, CommB) wygenerowanych przez DataInitLocal.

## Działanie

1. Pobiera wszystkie balloty z `BallotDatabase.Ballots`
2. Dla każdego typu commitmenty (CommC0, CommC1, CommB):
   - Pobiera commitmenty
   - Haszuje każdy commitment (SHA256)
   - Buduje drzewo Merkle (łączy pary, haszuje parentów)
   - Zapisuje wszystkie węzły i korzeń do `MerkleTreeDatabase`

## Struktura bazy danych

**TreeNodes** - wszystkie węzły drzewa:
- `TreeLevel`: 0 = liście, 1 = rodzice, itd.
- `Position`: pozycja na tym poziomie
- `Hash`: hash węzła
- `CommitmentType`: CommC0, CommC1, lub CommB
- `Phase`: faza (aktualnie 1)

**TreeRoots** - korzenie drzew:
- `CommitmentType`: CommC0, CommC1, lub CommB
- `RootHash`: korzeń drzewa
- `TotalLeaves`: liczba liści
- `Phase`: faza

## Uruchomienie

```bash
cd voteCodeServers/MerkleTreeBuilder
dotnet run
```

Lub:
```bash
./build_trees.sh
```

## Procedura

1. Uruchom DataInitLocal (generuje commitmenty w BallotDatabase)
2. Uruchom MerkleTreeBuilder (buduje drzewa)
3. Drzewa są gotowe do użytku - korzenie możesz wysłać na BB

## Haszowanie

- Liście: SHA256(commitment)
- Węzły: SHA256(left_hash + right_hash)
- Jeśli nieparzysta liczba węzłów, ostatni duplikuje się
