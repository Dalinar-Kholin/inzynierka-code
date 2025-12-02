po co user podpisuje ważne requesty

może wydarzyć się sytuacja gdzie server będzie chciał utrudniać wybory w pewnych regionach poprzez przekłamywanie
otrzymanych przez usera requestów
w takiej sytuacji:

S - server \
V - voter \
R(a) - request a

V wysyła R(a) --> S widzi że głos jest z regionu gdzie ma szacunkowo mniejsze poparcie --> R zmienia R(a) na R(a') --> S
odrzuca R(a') i zwraca błąd do V że request R(a') jest niepoprawny

w momencie gdy nie mamy podpisanych requestów S może to wykonać ale w sytuacji podpisanych requestów V sprawdzi czy jego
podpis się zgadza, wtedy aby S mógł przeprowadzić taką operacje musiałby złamać klucz V, co uważamy za trudny problem

tak wygląda request

```json
{
  "body": <T>,
  "sign" : "zV6lRdvxDF8MdYgUyy5SfFesdvUNu0xmtQ1tM5vRnDpkqs39ghsj8eJFeWDzYcQ7+SLMmtEWGv4JbygQjuWjBg=="
}
```


taki jest response

```json
{
  "body": {
    "userRequest": {
      "body": <T>,
      "sign": "zV6lRdvxDF8MdYgUyy5SfFesdvUNu0xmtQ1tM5vRnDpkqs39ghsj8eJFeWDzYcQ7+SLMmtEWGv4JbygQjuWjBg=="
    },
    "content": {
      "n": "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E088A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7EDEE386BFB5A899FA5AE9F24117C4B1FE649286651ECE65381FFFFFFFFFFFFFFFF",
      "g": "02",
      "c": "86c7d01c93bbc3f1285b747962d31a81c1ffff492b300edaa588d30889bf2dfd1fed363d83247b55bc22edcf39987add0a82efc9aff33beb0159f93206c401b217f4a09f0eeae31784031cf7dae9671b68c257375fff7576400afc840df5ca08545b5db7306836ba7378d211e937c033531490ff17b314b89131de92789c235b"
    }
  },
  "sign": "vzfOoCKM390bhUpzhd86tlZlQL54YUvhHnk50dNO2bBYMazNlUYJK5GjTXv2z5CNa9zK6YQh+bocjDayyJl1Bw=="
}
```


dzięki temu server nie może modyfikować requesta V inaczej w Response który został przez niego podpisany będzie błąd, a user ma dowód że response z servera jest poprawny, jest podpisany więc server nie może zwracać złych reponsów zasłaniając się błędami, ponieważ jest podpis


server nie musi odrzucać źle podpisanych requestów, na tym powinno zależeć V, podpis nie uprawnia absolutnie do nieczego, służy tylko do sprawdzenia servera, bezpieczeństwo głosowania dalej opiera się na podpisach zaufanych i entropi kart do głosowania