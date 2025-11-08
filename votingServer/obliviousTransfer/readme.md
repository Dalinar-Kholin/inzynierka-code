tak na fajście jak to działa


użytkownik rozpoczyna komunikacje od przekazania authSerial karty do głosowania

server ma zcommitowane paczki authCodów w postaci
[2]authCode, c

gdzie c jest takie że jest to klucz prywatny DHE

server odsyła do użytkownika

C = g^c % p
gdzie C to pubKey

teraz użytkownik musi wygeberować 2 klucze publiczne takie że 