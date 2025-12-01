pierwszym kodem dostępu będzie authCode -> bazując na jego entropi $64^{64}$
oraz na tym że aby go pobrać trzeba się uwierzytelnić przed serwerem EA(pobranie kart do głosowania)

następnie server będzie generował kolejny losowy ciąg znaków też $64^{64}$ którego jedynym zadaniem
będzie pozwolenie na podpisanie transakcji
server po otrzymaniu takiego kodu NIE MOŻE NIE PODPISAĆ TRANSAKCJI jeśli program id oraz otrzymany kod jest poprawny

otrzymywanie kodu odbywa się poprzez podpisany request
więc server nie może oszukiwać na nie podpisywanu
ponieważ podpisany request ujawnia że EA stara się oszukać
