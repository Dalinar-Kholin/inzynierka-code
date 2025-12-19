
użytkownika pobiera kartę na której ma dostęp do voteCodów oraz lockCode(to co mówi że głos nie jest fake)

użytkownik pobiera za pamocą OT authCode oraz

uzytkownik chce umieścić na tablicy fake głos więc wysyła krotkę na BB

(voteCode, authCode, szyforgram("jebać te wybory"(lub inną równie wartościową dla przeprowadzających wybory wiadomości)))

wysyła authCode do serwera aby zaakceptował ten głos

server nie wie czy głos jest fake czy nie bo nie posiada możliwości odszyfrowania 3 pozycji w krotce

serwer akceptuje ten głos i wrzuca potrzebne dane na BB

user podpisuje(albo i nie) ten głos podpisem zaufanym --> to powie serverą żeby pokazały commitmenty jeśli głos nie zawiera poprawnego lockCode

user może powtórzyć protokół tylko wysłać poprawny lockCode


user dowie się jak został zinterpretowany głos dopiero po wyborach ale to i tak w sumie jest git(nie każdy jest tiktokowcem który potrzebuje mieć wszystko teraz)