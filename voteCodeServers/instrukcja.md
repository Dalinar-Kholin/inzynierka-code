Krok 1:
~/inzynierka-code/voteCodeServers/DataInitLocal ./run_all_data_init.sh
Trzeba sparwdzić w danych czy wszystkie wygenerowane

Krok 2:
~/inzynierka-code/voteCodeServers/BallotDataLocal ./run_all_ballot_data.sh
Trzeba sparwdzić w danych czy wszystkie wygenerowane

Krok 3:
~/inzynierka-code/voteCodeServers/VoteCodesGenerator ./start_servers.sh
Powinna się wtedy konsola odpalić co wyświetla wszystkie 10 serwerów
i na konsoli 1 (powinna to być 5001) trzeba wpisać: "init"

Ostatni zapis do bazy danych może trwać 30s (bo taki timeout jest ustawiony, żeby sie nie zapisywało po 5-10 rekordow tylko np po 1k).
Może również wystąpić błąd z paillierem (bo wrapper czasami nie możę znaleźć kodu cpp i trzeba mu to gdzieś dać żeby znalazł)