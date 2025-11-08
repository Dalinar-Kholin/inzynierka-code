tak na fajście jak to działa


użytkownik rozpoczyna komunikacje od przekazania authSerial karty do głosowania

server ma zcommitowane paczki authCodów w postaci
[2]authCode, c

gdzie c jest takie że jest to klucz prywatny DHE

server odsyła do użytkownika

C = g^c % p
gdzie C to pubKey

teraz użytkownik musi wygeberować 2 klucze publiczne takie że

A = g^a % p ; a = priv key
B = A^-1 × C

I zwraca te klucze do serwera

server tworzy 2 klucze prywatne xa, xb następnie generuje wspólny sekret dla każdej pary kluczy, za pomocą wspólnego sekretu tworzy szyfrogram AES z authCodow i wysyła go do użytkownika
użytkownik za pomocą klucza deszyfruje szyfrowanie i ma authCode

dlaczego to dziala, dlatego ze

C = A×B
g^c mod p= g^a × g^b mod p
Aby poznac klucz prywatny od b user musiałby odwrócić g^c%p a to jest złożony problem
