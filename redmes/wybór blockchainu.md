## Wybór technologii blockchain

Na początku realizacji pracy inżynierskiej sformułowano wymagania dotyczące technologii blockchain, na której miał zostać oparty projektowany system.  
Wymagania te wynikały zarówno z charakteru projektowanego rozwiązania, jak i z konieczności zapewnienia jego użyteczności, skalowalności oraz bezpieczeństwa.

Przyjęto następujące **kryteria wyboru**:

- wysoka szybkość przetwarzania transakcji,
- niskie koszty operacyjne, w szczególności niskie opłaty transakcyjne,
- możliwość realizacji transakcji typu *gasless*, czyli bez konieczności posiadania przez użytkownika natywnego tokena sieci,
- wystarczająco wysoka popularność i dojrzałość ekosystemu, tak aby koszt potencjalnego ataku na sam blockchain był możliwie wysoki.

Na podstawie powyższych kryteriów przeanalizowano dostępne i powszechnie stosowane rozwiązania blockchainowe.  
Do dalszych rozważań wybrano trzy technologie spełniające podstawowe założenia projektu:

- **Ethereum** oraz blockchainy kompatybilne z **EVM (Ethereum Virtual Machine)**,
- **Solana**,
- **BNB Smart Chain (BSC)**.

Po wstępnej selekcji trzech technologii blockchain (Ethereum/EVM, BNB Smart Chain oraz Solana) przeprowadzono ich pogłębioną analizę z punktu widzenia projektowanego systemu.  
Oprócz wcześniej wskazanych kryteriów, uwzględniono również dodatkowe aspekty istotne w kontekście długoterminowej eksploatacji systemu oraz bezpieczeństwa jego działania.

---

## Model konsensusu i bezpieczeństwo sieci

Jednym z kluczowych elementów wpływających na bezpieczeństwo blockchaina jest zastosowany mechanizm konsensusu.

### Ethereum
Ethereum wykorzystuje mechanizm **Proof of Stake (PoS)**, oparty na bardzo dużej liczbie walidatorów oraz wysokim koszcie ekonomicznym potencjalnego ataku.  
Sieć cechuje się najwyższym poziomem decentralizacji spośród analizowanych rozwiązań, co bezpośrednio przekłada się na odporność na ataki typu *51%*.

### BNB Smart Chain
BNB Smart Chain również opiera się na PoS, jednak w praktyce stosowany jest model **Proof of Staked Authority (PoSA)**, w którym liczba walidatorów jest ograniczona.  
Skutkuje to wyższą przepustowością, lecz kosztem niższego poziomu decentralizacji i większego zaufania do podmiotów zarządzających siecią.

### Solana
Solana wykorzystuje hybrydowy mechanizm **Proof of History (PoH)** w połączeniu z PoS, co umożliwia bardzo szybkie porządkowanie transakcji w czasie.  
Rozwiązanie to zapewnia wysoką wydajność, jednak odbywa się kosztem większych wymagań sprzętowych dla walidatorów, co może wpływać na poziom decentralizacji.

Z punktu widzenia projektowanego systemu, bezpieczeństwo sieci Solana uznano za wystarczające, przy jednoczesnym zachowaniu bardzo wysokiej wydajności.

---

## Wydajność i skalowalność

Istotnym kryterium była zdolność blockchaina do obsługi dużej liczby transakcji w krótkim czasie.

### Ethereum
Ethereum w warstwie bazowej charakteryzuje się relatywnie niską przepustowością.  
Skalowalność osiągana jest głównie poprzez rozwiązania warstwy drugiej (*Layer 2*), takie jak rollupy, co jednak komplikuje architekturę systemu.

### BNB Smart Chain
BNB Smart Chain oferuje wyższą przepustowość niż Ethereum, jednak nadal pozostaje ograniczony architekturą EVM i czasem produkcji bloków.

### Solana
Solana została zaprojektowana jako blockchain wysokiej przepustowości, umożliwiający przetwarzanie tysięcy transakcji na sekundę bez stosowania dodatkowych warstw skalujących.  
Cecha ta jest szczególnie istotna w systemach wymagających niskich opóźnień i wysokiej responsywności.

---

## Koszty transakcyjne i ich przewidywalność

Koszty operacyjne systemu blockchainowego mają bezpośredni wpływ na jego użyteczność.

- W sieci **Ethereum** opłaty transakcyjne są zmienne i silnie zależne od aktualnego obciążenia sieci, co utrudnia przewidywanie kosztów działania systemu.
- **BNB Smart Chain** oferuje niskie i stabilne opłaty, co czyni go atrakcyjnym z punktu widzenia ekonomicznego.
- **Solana** charakteryzuje się bardzo niskimi kosztami transakcyjnymi, jednak ich wysokość może być mniej przewidywalna w momentach dużego obciążenia sieci. Mimo to koszty pozostają istotnie niższe niż w przypadku Ethereum.

---

## Model programowania smart contractów

Język oraz model programowania smart contractów mają istotny wpływ na bezpieczeństwo i jakość implementacji.

- **Ethereum** oraz **BNB Smart Chain** wykorzystują język **Solidity**, który jest szeroko rozpowszechniony, lecz podatny na klasyczne błędy programistyczne (np. *reentrancy*, błędy związane z zarządzaniem pamięcią).
- **Solana** umożliwia pisanie smart contractów w języku **Rust**, który zapewnia silny system typów, kontrolę nad pamięcią oraz większe bezpieczeństwo na etapie kompilacji. Jest to istotna zaleta w kontekście systemów o podwyższonych wymaganiach bezpieczeństwa.

---

## Obsługa transakcji typu gasless

Obsługa transakcji typu *gasless* była jednym z kluczowych wymagań projektowych.

- W ekosystemie **Ethereum** realizacja transakcji gasless wymaga zastosowania dodatkowych mechanizmów, takich jak meta-transakcje (EIP-2771) lub konta abstrakcyjne (EIP-4337), co zwiększa złożoność systemu.
- **BNB Smart Chain**, jako sieć kompatybilna z EVM, podlega tym samym ograniczeniom co Ethereum i również wymaga warstw pośrednich do realizacji transakcji gasless.
- **Solana** oferuje natywne wsparcie dla delegowania opłat transakcyjnych, co pozwala na prostą implementację transakcji gasless bez potrzeby stosowania dodatkowych standardów czy kontraktów pośredniczących.

---

## Dojrzałość ekosystemu i wsparcie deweloperskie

- **Ethereum** posiada najbardziej rozwinięty ekosystem narzędzi, bibliotek oraz dokumentacji, co ułatwia rozwój i utrzymanie systemów blockchainowych.
- **BNB Smart Chain** korzysta bezpośrednio z ekosystemu Ethereum, oferując dobrą dostępność narzędzi przy niższych kosztach.
- **Solana** posiada mniejszy, lecz dynamicznie rozwijający się ekosystem. Dostępne narzędzia są wystarczające do realizacji projektu, choć wymagają większej wiedzy technicznej od zespołu deweloperskiego.

---

## Podsumowanie porównania

Uwzględniając wszystkie analizowane kryteria — w szczególności wydajność, koszty operacyjne, bezpieczeństwo, model programowania oraz natywne wsparcie dla transakcji typu gasless — **Solana** została uznana za technologię najlepiej dopasowaną do założeń projektowych pracy inżynierskiej.

Jej architektura umożliwia realizację systemu o wysokiej wydajności i niskich kosztach, przy jednoczesnym uproszczeniu architektury aplikacyjnej dzięki natywnemu mechanizmowi transakcji gasless.
