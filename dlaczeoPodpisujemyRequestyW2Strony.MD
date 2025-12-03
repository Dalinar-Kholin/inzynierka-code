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
gdzie <T> to dane zapytanie a <E> dane Response
taki jest response

```json
{
  "body": {
    "userRequest": {
      "body": <T>,
      "sign": "zV6lRdvxDF8MdYgUyy5SfFesdvUNu0xmtQ1tM5vRnDpkqs39ghsj8eJFeWDzYcQ7+SLMmtEWGv4JbygQjuWjBg=="
    },
    "content": <E>
  },
  "sign": "vzfOoCKM390bhUpzhd86tlZlQL54YUvhHnk50dNO2bBYMazNlUYJK5GjTXv2z5CNa9zK6YQh+bocjDayyJl1Bw=="
}
```


dzięki temu server nie może modyfikować requesta V inaczej w Response który został przez niego podpisany będzie błąd, a user ma dowód że response z servera jest poprawny, jest podpisany więc server nie może zwracać złych reponsów zasłaniając się błędami, ponieważ jest podpis


server nie musi odrzucać źle podpisanych requestów, na tym powinno zależeć V, podpis nie uprawnia absolutnie do nieczego, służy tylko do sprawdzenia servera, bezpieczeństwo głosowania dalej opiera się na podpisach zaufanych i entropi kart do głosowania