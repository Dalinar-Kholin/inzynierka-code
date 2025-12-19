pierw zdefiniujmy aktorów głosowania

Biuletyn Board(BB) - Smartcontract solany mający w swoim stanie wszystkie dane które powinny być publiczne oraz niemodyfikowalne(z jednym wyjątkiem)

Trustee(T) - zaufane serwery które wspólnie odpowiadają za wygenerowanie votecodów oraz ostateczne zliczenie wyników głosowania

Voter(V) - obywatel upoważniony prawnie do głosowania który spełnia warunek
braku upośledzenia(wskazane) oraz
potrafiący korzystac z podpisu dowodem osobisty(wymagane) i
ma dostęp do yubiKey PIV (Smart Card – PKCS#11) z możliwością deszyfrowania danych

Voting Serwer(EA) - serwer należący do EA z którym będzie komunikował się użytkownika w celu przechodzenia przez kolejne etapy głosowania

Signing server(SS) - server odpowiadający za podpisywanie transakcji mających zmodyfikować stan BB

Sign Verifier(SV) - server odpowiadający za sprawdzenie poprawności podpisu dowodem osobistym

Voting Device(VD) - główne urządzenie z którego V będzie głosować

Helper(H) - urządzenie pomocnicze pozwalające V przeprowadzić bezpieczene głosowanie w przypadku gdy H i VD to jedno urządzenie, głosowanie może nie być bezpieczne

Commit() - akcja polegająca na wysłaniu commitmentów na BB tak aby później można było sprawdzić poprawność głosowania

PKG - klucz publiczny głosowania, SKG - klucz prywatny głosowania

Inicjacja głosowania:\
T - generują voteCody pozwalające V zagłosować na odpowiedniego kandydata - na razie nie przekazują swoich shardów voteCodu do EA\
T - Commit(voteCody)

EA - generuje karty do głosowania zaweierjące (voteSerial, authSerial, [4]AuthCodePack, lockCode)\
gdzie authCode Pack to
```json
{
  "authCodes" : [2]authCode,
  "c" : liczba c pozwalająca na oblivious transfer
}
```
liczbe c generujemy już teraz a nie w momencie OT aby zmniejszyć ilość commitmentów na BB, jeśli wygenerujemy je już teraz, możemy z tych pakietów stworzyć drzewo merkele\
które pozwoli zcommitować wszystko na raz a jednocześnie na BB wystarczy tylko wysłać korzeń tego drzewa, oszczędzając nam kilka miliardów złotych w solanie na commitmentach\

gdybyśmy tego nie robili, EA przy OT pierw musiło by losowac c mastępnie pojedyńczo commitować, po czym V musiałby sprawdzić czy taki commitment się pojawił, co spowolniło by cały proces oraz astronomicznie podwyższyło koszta całego głosowania

EA - Commit(AuthCard)\
EA - Commit(PKG) - klucz tego głosowania którym EA będzie podpisywało swoje odpowiedzi aby móc zweryfikować odpowiedź servera,
oraz chroniąca przed replay attack aby przy pobieraniu karty była jakaś wartość różna od poprzednich wyborów


protokół głosowania dla V:


V tutaj używa H\
V generuje XML zawierający
```xml
<?xml version="1.0" encoding="UTF-8"?>
<Gime>
    <EAkey>PKG</EAkey>
    <voterPublicKey>voter public key pobrany z yubi key</voterPublicKey>
</Gime>
```
następnie za pomocą eDO App podpisuje dowodem osobistym ten XML

V wysyła do EA podpisaną prośbę o kartę do głosowania

EA wysyła do SV które sprawdza czy podpis jest poprawny jeśli jest EA zwraca do V wylosowaną losową kartę do głosowania

od teraz każda wymiana wiadomości jest podpisywana w sposób\
voter request
```json
{
  "body" : "request body",
  "sign": "request podpisany za pomocą klucza wysłanego do EA w prośbie o pakiet"
} 
```

EA response
```json
 {
  "body": {
    "voter request": "kopia requestu wysłanego przez V",
    "body response": "odpowiedź servera"
  },
  "sign": "podpisane body za pomocą PKG"
}
```
od tego momentu każda strona zapisuje requesty aby później móc potwierdzić nieprawidłowości w przebiegu głosowania za pomocą podpisanych requestów

teraz V przechodzi do VD\
V przepisuje z H authSerial i voteSerial\
V wybiera który w kolejności 0,1 auth Code chce spalić podczas OT\
V przeprowadza OT(tutaj link do rozdziału o OT) aby pobrać od EA authCode pozwalający oddać głos\

V tworzy transakcje Solany która umieszcza na BB krotkę\
jeśli V chce aby ten głos był jego ostatecznym głosem który będzie zliczony\
(voteSerial, voteCode, authCode, enc(lockCode)) -- tutaj do szyfrowania będzie użyty kolejny klucz tresholdowy ale to później się wyklaruje\
jeśli V chce sprawdzić uczciwość Trustee
(voteSerial, voteCode, authCode, enc("nie wierzę w uczciwość")) -- zaenkodowana wiadomość musi mieć 1 własność, musi nie być lockCodem a poza tym to może być cokolwiek\

V wysyła do SS transakcje aby SS opłaciło wszystkie podatki oraz koszty obliczeń dla tej transakcji\
!!! tutaj jest krótkie okno w którym authCode jeszcze nie jest publiczny bo nie ma go na BB, a jednak zakładając współprace SS i EA, EA może się dowiedzić jaki jest authCode mimo OT
ale to i tak nic nie daje, bo w razie gdy użytkownik zobaczy na BB inna krotkę niż on chciał może prosić server o pokazanie podpisanego przez niego requestu, czego server nie może zrobić bo nie zna SK V

V dostaje podpisaną transakcje a następnie wysyła ją do blockchainu solany

V wysyła do EA voteCode aby ten odpowiedźiał i potwierdził głos

EA sprawdza czy wszystkie dane na BB są spójne z tym co EA wygenerował podczas init\
jeśli tak dodaje do tej krotki swój podpis SKG\
podpisaną krotkę umieszcza na BB

V z poziomu H\
V sprawdza czy podpis w krotce się zgadza\
jeśli V chce aby jego głos był interpretowany pobiera XML tej krotki a następnie podpisuje ją za pomocą dowodu osobistego

to jest koniec gosowania dla V


koniec wyborów:

trustee zaczynają interpretować głosy które przeszły przez wszystkie etapy głosowania oraz posiadają ostateczny podpis\
jeśli lockCode jest poprawny zliczają głos(tutaj link do rozdziału o liczeniu głosów)\
jeśli lockCode nie jest poprawny, trustee odkrywają swoje commitmenty dla tego VoteCodu\
jeśli 2 głosy mają ten sam lockCode obydwa głosy są ignorowane\

EA odkrywa wszystkie swoje commitmenty od razu, ponieważ nie ma niczego co po wyborach byłoby do ukrycia

wszyscy mogą teraz zweryfikować czy głosowanie odbyło się poprawnie oraz czy nie ma żadnych nieprawidłowości w commitmentach







