w naszym systemie krytyczną funkcjonalnością jest oblivious transfer authCode

zdecydowaliśmy się na skorzystanie z protokołu oblivious transfer przy wykorzystaniu elgamala plus szyfrowania AES

protokół

na początku mamy:
generator g = 2 \
liczba pierwsza P taka że jest zgodan z RFC 7919 z siłą grupy na poziomie 192 bitów


v - voter
s - voting server

v --> s\
init\
s --> v\
server losuje wartość c a następnie generuje klucz publiczny 
w postaci $C = g^c\ mod \ P$ \
v <-- C


voter losuje a a nstępnie oblicza klucz publiczny A | $A = g^a\ mod \ P$\
po czym liczy drugi klucz publiczny B | $B = AC^{-1}$\
s <-- A,B

server pobiera 2 authCody, auth1, auth2 i szyfruje kolejno kluczami publicznymi
> serwer nie może rozróżnić który klucz jest prawdziwy a który fałszywy ponieważ B jest prawdziwym kluczem publicznym z istniejącym kluczem prywatnym
> tylko v nie może odwrócić tego klucza bo jest to obliczeniowo ciężkie
> 
> więc server nie wie który klucz został przesłany a który został spalony

serwer losuje z1, z2 i oblicza $Zn = g^{zn}\ mod\ P$\
za pomocą Zn oblicza wspólny sekret z A i B\
na bazie wspólnego sekretu inicjuje szyfrowanie AES którym szyfruje auth1, auth2

Z1A - wspólny serkret z kluczy A i Z1
Z2B - wspólnySekret z kluczy B i Z2

v <-- encAES(ZA, auth1), encAES(ZB, enc2), Z1, Z1, AesNonce1, AesNonce2\
voter jest w stanie odszyfrować dokładnie jeden szyfrogram i użyć go jako auth code


$G^a$