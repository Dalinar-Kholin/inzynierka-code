# ReleyServer
Serwer przekazujący częściowe odszyfrowania kart do głosowania

## Instalacja
pip install -r requirements.txt

## Uruchomienie
python server.py <serverId>

Przykłady:
python server.py 1  # Uruchamia serwer z ID=1 na porcie 5001
python server.py 2  # Uruchamia serwer z ID=2 na porcie 5002
python server.py 3  # Uruchamia serwer z ID=3 na porcie 5003

## Uruchomienie wszystkie serwery do testów
./run_all_relay_servers.sh
aby zatrzymać: ./stop_all_relay_servers.sh

## Uruchomienie bez bazy danych (3 dane testowe)
python server.py <serverId> test

## Uruchomienie wszystkich serwerów bez bazy danych (3 dane testowe)
w ./run_all_relay_servers.sh dodać "test"
./run_all_relay_servers.sh
aby zatrzymać: ./stop_all_relay_servers.sh
