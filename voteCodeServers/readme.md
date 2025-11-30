voteCodesGenerator - uruchamiany przed glosowanie do generowania ciphertextu karty z voteCodami. Każdy wygenerowany ciphertext gdzieś zapisujemy (chyba trzeba na bb zeby byla pewnosc ze nic nie zostalo zmienione - ale trzeba domyśleć bo to za dużo bedzie zajmować). 
Każdy głosujący powinien dostać przynajmniej 2 karty. Jedna to faktycznego głosowania, a druga do audytu.

tallyingServer - uruchamiany podczas głosowania do sprawdzania czy dany voteCode z bb jest poprawny oraz wykonujemy wstępne zliczenie (zamiana na wektor homomorficzny - osobne klucze pailliera trzeba wygenerować tylko do tego celu - podzielenie klucza prywatnego na jakieś zaufane strony).
Może to też w tym samym czasie obsługiwać deszyfracje ciphertextu karty do głosowania (chyba, że zakładamy, że głosujący przed rozpoczęciem głosowania może już dostać swoje vc). Każdy serwer wtedy musi mieć "kontakt" z głosującym by wysłać mu części potrzebne do odszyfrowania (jakbyśmy to robili przez jakiś 1 serwer to wtedy ten serwer by wszystko poznał - zaprzecza to całemu protokołowi printegrity). Na froncie będziemy potrzebowali funkcji do łączenia cześci kart.

voteCodesGenerator generuje map, a tallyingServer z niej korzysta.


Trzeba pamiętać, że odkrywanie commitmentów to nie odkrywanie wszystkich danych z "i".
Jeden wiesz "i" zawiera dane 2 kart np, s' nie jest zwiazany

Można pomyśleć ile chcemy mieć maksymalnie głosujących, bo można na spokojnie tworzyć permutacje 100mln liczb, ale już większej ilości robi się ciężka. Jedynie dało by się to zwiększyć batchami - wtedy też jest to raczej bezpieczne, ale powinny być dość spore. W sumie można protokół odpalić kilka razy i to tak samo bedzie działać, więc jest raczej szybkie do wdrożenia jak coś.


===========================================================================================
Na podstawie danych dostarczonych przez EA:
===========================================================================================
- alfabet A
- kandydaci k
- kolejność serwerów
- liczba głosujących N
- parametr bezpieczeństwa n 
publikuje i podpisuje thresholdowym kluczem publicznych (chyba ten sam co do zliczania)

===========================================================================================
1. DataInit - lokalnie
===========================================================================================
PART I
Każdy serwer j dla każdego i z {1,...,4N+2n}:
- generuje shadow serial s {1,...,4N+2n}
- losowo wybiera 2 elementy alfabetu A
- generuje dla każdego kandydata bity b^p_k {0,1}
- publikuje wiersz: i, shadow serial, comm(element_1), comm(element_2), comm(b^p_1,...,b^p_k)

PART II
Każdy serwer j dla każdego i:
- jeżeli j nie jest ostatnie - generuje shadow serial s' {1,...,4N+2n}
- jeżeli j ostatnie - generuje S już jakoś losowo, żeby było cięzkie do zgadnięcia, ale łatwe do wpisania
- publikuje wiersz: i, s'/S

===========================================================================================
2. DataLinking - lokalnie
===========================================================================================
input to lista seriali serwera j-1, ale wiemy, że seriale są z {1,...,4N+2n} więc bez sensu to jest
robimy bez wejścia
Każdy serwer j:
- generuje losowo permutację π (łączącą s{j-1}i' z s{j}i)
  tzn. dostajemy s_serial od j-1, sprawdzam w permutacjach jakie "i" oznacza i bierzemy odpowiedni s_serial od i
- generuje losowo permutację π' (łączącą s'{j-1}i' z s'{j}i)
- publikuje wiersz: i, comm(s{j-1}i') - zobowiązanie do permutacji
- publikuje wiersz: s'{j}i, comm(s'{j-1}i') - zobowiązanie do permutacji
(mamy powiazanie między i, a s'{j}i więc nie wiadomo czemu raz jest i, a raz s'{j}i)

Każdy serwer u siebie zapisuje po 2 kombinacje do każdej permutacji (lub po dwa indeksy)

Przed 3 etapem trzeba wrzucić peirwszy korzeń merkla na bc
===========================================================================================
3. SummandsDraw - lokalnie (tzn. trzeba pobrać skądś tą wartość losową lub stworzyc wlasny serwer co bedzie to zwracać )
===========================================================================================
pobranie wartości losowej z bb (jest ona dostarczana na bb tylko wtedy gdy każdy serwer zrobi etapy 1 i 2).
Na podstawie tej losowej wartosci liczone sa c i b

===========================================================================================
4. CodeSetting - lokalnie
===========================================================================================
dość złożone dużo commitmentow

===========================================================================================
5. Pre-PrintAudit
===========================================================================================
w 4. wsm sie juz robi wszystkie potrzebne dane. Tutaj bedzie tylko sprawdzenie każdej grupy...

===========================================================================================
6. BallotPrinting
===========================================================================================
proste ale wymaga komunikacji kazdego serwera + paillier

===========================================================================================
7. PrintAudit
===========================================================================================

===========================================================================================
8. Przekazanie kart głosującym
===========================================================================================
ktos zaufany daje im rozkaz zeby kazdy serwer zrobil partial decryption i wysłał to WPROST 
do głosującego

===========================================================================================
8. Tallying
===========================================================================================
najgorsze, komunikacja miedzy serwerami + dużo dowodów poprawności + szyfrowanie elGamala

===========================================================================================
9. PostElectionAudit
===========================================================================================
upublicznienie wszystkich zobowiązań do kart nieużytych


Podzielenie na części:
- offline - data init,data linking, summands draw, code setting
- online - pre-print audyt - sprawdzenie czy kody sa unikalne
- online - generowanie kart na podstawie data init
- online - tallying