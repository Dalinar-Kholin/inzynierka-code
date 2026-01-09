1 spytac zagóra czy można trzymać klucze prywatne na froncie i wtedy:

shardy sa szyfrowane kluczem pyublicznym usera i przy pobieraniu karty dawane razem z nią --> user odszyforwuje shardy za pomocą klucza prywatnego na froncie(specjalnej apce ale to raczzej bez sensu)


napisać kontener który wygeneruje randomes beacon na BB aby trustee mogły sobie go pobrać(na całe głosowanie potrzebny będzie tylko 1 więc nie ma problemu z archiwizacją starych beaconóœ)
sprawdza czy commitmenty sa wygenerowane i jeżeli są to generuje, jeśli nei sa to się wyłącza czeka minute i tka aż do powstania commitmentów


poprawić implementacje commitera aby trustee mogło z nich korzysatać

https://people.csail.mit.edu/silvio/Selected%20Scientific%20Papers/Pseudo%20Randomness/Verifiable_Random_Functions.pdf



napisać prosty server który będzie porxy między BB a jsonem aby łatwo pobierać wartości z BB

napisać prosty przykładowy request który pokaże jak działa Commiter

napsiać joba który ściągnie z BB VS i VC poprawnych zweryfikowanych głosów i wyśle do vote servera Trustee

dodac 2 kartę dla V aby dało się ją odsłonić 

przesyłanie vektora głosu do BB

podpisy między serverami trustee (kochamy podpisy)