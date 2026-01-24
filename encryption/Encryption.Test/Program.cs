using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using Org.BouncyCastle.Math;
using VoteCodeServers.Helpers;

class Program
{
    static void Main(string[] args)
    {
        string _alphabet = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890";
        int _baseValue = _alphabet.Length;

        var publicKey = new PaillierPublicKey();
        BigInteger n_squared = publicKey.n_squared;

        var sharedKeys = new List<PaillierSharedKey>
        {

            new PaillierSharedKey(serverNumber: 1),
            new PaillierSharedKey(serverNumber: 2),
            new PaillierSharedKey(serverNumber: 3),
            new PaillierSharedKey(serverNumber: 4),
            new PaillierSharedKey(serverNumber: 5),
            new PaillierSharedKey(serverNumber: 6),
            new PaillierSharedKey(serverNumber: 7),
            new PaillierSharedKey(serverNumber: 8),
            new PaillierSharedKey(serverNumber: 9),
            new PaillierSharedKey(serverNumber: 10)
        };

        int thresholdPlayers = sharedKeys.First().degree + 1;

        var missingPlayers = Enumerable.Range(1, thresholdPlayers)
            .Where(id => !sharedKeys.Any(k => k.player_id == id))
            .ToList();

        if (missingPlayers.Any())
        {
            throw new InvalidOperationException($"Missing shares for players: {string.Join(",", missingPlayers)}");
        }

        PaillierSharedKey decryptKey = sharedKeys.First(k => k.player_id == 1);

        Dictionary<int, BigInteger> BuildPartialDecryptions(BigInteger ciphertext)
        {
            var partialDecryptions = new Dictionary<int, BigInteger>();
            foreach (var key in sharedKeys)
            {
                if (key.player_id <= thresholdPlayers)
                {
                    partialDecryptions[key.player_id] = key.partial_decrypt(ciphertext);
                }
            }
            return partialDecryptions;
        }

        // Użyj konstruktora BouncyCastle zamiast Parse
        BigInteger code1 = new BigInteger("1");
        BigInteger code2 = new BigInteger("1");
        BigInteger code3 = new BigInteger("1");
        BigInteger code4 = new BigInteger("1");

        var swTotal = System.Diagnostics.Stopwatch.StartNew();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        BigInteger ciphertext1 = publicKey.Encrypt(code1);
        BigInteger ciphertext2 = publicKey.Encrypt(code2);
        BigInteger ciphertext3 = publicKey.Encrypt(code3);
        BigInteger ciphertext4 = publicKey.Encrypt(code4);

        sw.Stop();
        long time = sw.ElapsedMilliseconds;
        Console.WriteLine($"czas = {time}\n");

        // HOMOMORFICZNE DODAWANIE: mnożenie szyfrogramów
        ciphertext1 = ciphertext1.ModPow(BigInteger.One, n_squared);
        var partialDecryptions1 = BuildPartialDecryptions(ciphertext1);
        ciphertext2 = ciphertext2.ModPow(BigInteger.One, n_squared);
        var partialDecryptions2 = BuildPartialDecryptions(ciphertext2);
        ciphertext3 = ciphertext3.ModPow(BigInteger.One, n_squared);
        var partialDecryptions3 = BuildPartialDecryptions(ciphertext3);
        ciphertext4 = publicKey.Encrypt(BigInteger.Zero);
        var partialDecryptions4 = BuildPartialDecryptions(ciphertext4);

        Console.WriteLine($"c1: {ciphertext1}");
        Console.WriteLine($"Odszyfrowane c1: {decryptKey.decrypt(partialDecryptions1)}");
        Console.WriteLine($"c2: {ciphertext2}");
        Console.WriteLine($"Odszyfrowane c2: {decryptKey.decrypt(partialDecryptions2)}");
        Console.WriteLine($"c3: {ciphertext3}");
        Console.WriteLine($"Odszyfrowane c3: {decryptKey.decrypt(partialDecryptions3)}");
        Console.WriteLine($"c4: {ciphertext4}");
        Console.WriteLine($"Odszyfrowane c4: {decryptKey.decrypt(partialDecryptions4)}");

        BigInteger ciphertext_sum = ciphertext1.Multiply(ciphertext2).Mod(n_squared)
            .Multiply(ciphertext3).Mod(n_squared)
            .Multiply(ciphertext4).Mod(n_squared);

        // Oczekiwana suma - używaj metod BouncyCastle
        BigInteger expectedSum = code1.Add(code2).Add(code3).Add(code4);
        Console.WriteLine($"Oczekiwana suma: {expectedSum}");
        Console.WriteLine($"Ciphertext sum: {ciphertext_sum}");

        var partialDecryptions = BuildPartialDecryptions(ciphertext_sum);

        Console.WriteLine($"Odszyfrowane przed re-encryption: {decryptKey.decrypt(partialDecryptions)}");

        Console.WriteLine($"c1: {ciphertext_sum}");

        ciphertext_sum = publicKey.ReEncrypt(ciphertext_sum);

        Console.WriteLine($"c2: {ciphertext_sum}");



        partialDecryptions = BuildPartialDecryptions(ciphertext_sum);

        Console.WriteLine("====================");

        // Om6tX0ZJir 0000c ROROO 6x6xx 20022 iPiPi vvvav QBBBQ ZZ22Z HH444 pOpOO
        // 0R62ivQZHp

        // Zamienić potem żeby decrypt był statyczny
        Console.WriteLine($"Odszyfrowane: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c1 = new BigInteger("295712997556115453298523502016162156425990017637954057290662167102345952042180301873922195883925109377349926342093541068140351125029493771269665164965214182140333935090422595534726008084219148850341821318899102449700703812989337671396565064247867236335532794308827754083992684621633242505257158952794630997050552");

        partialDecryptions = BuildPartialDecryptions(c1);

        Console.WriteLine($"Odszyfrowane1: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c2 = new BigInteger("198847907876867986748269001422154099106407410606791328131051232913676542391359848509557573185846917289663331314294465000382104740410776876855307849678832632982071343753306533616259700238226758181678205879487028820086531861835949735108988173595844150156054030235783954841521992619426675486213066057877914152241893");

        partialDecryptions = BuildPartialDecryptions(c2);

        Console.WriteLine($"Odszyfrowane2: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c3 = new BigInteger("131677582986837754845044684114902668306943585018457909942863513713650197984850042389537097962991394919670934724239403497314449415492187860669571171638436492647286422963319744286375133740132728921408644097968656671066227283823473397714326313284803598136697760252679899423161726029353058452198022183046603082526365");

        partialDecryptions = BuildPartialDecryptions(c3);

        Console.WriteLine($"Odszyfrowane3: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c4 = new BigInteger("384110197510642060741392411198985047326916872684600419245060966001494615742752411144090088594868835407888180339033686284955316966576484568003661815436135116222582833305580413551559408953534281369897821112897315767744625479123801315568266047188221680341559014360210709055133711350864761393075543305770046824378656");

        partialDecryptions = BuildPartialDecryptions(c4);

        Console.WriteLine($"Odszyfrowane4: {decryptKey.decrypt(partialDecryptions)}");

        BigInteger c5 = new BigInteger("84164178258201290503200810913795285857768396508919699508808454032528727530031936508705742187112123966325994597906106748408078321038354810304574060178238445507083055389996625790660701663415136074174127966431688856614112333660866793760597273946454178020267286283426135844380046632571911769656157678422032505097064");
        partialDecryptions = BuildPartialDecryptions(c5);

        var decrypted5 = decryptKey.decrypt(partialDecryptions);
        var decrypted = Decode(new BigInteger(decrypted5.ToString()));

        Console.WriteLine($"Odszyfrowane5: {decrypted5}");
        // dEHm8jBYiR TTyTT ddd4d offfo yyyyy MMbMM eddee IIIDD r1r11 mZZmZ ffXfX
        // TdoyMeIrmf

        // Console.WriteLine("\n=== RĘCZNE ŁĄCZENIE PARTIAL DECRYPTIONS ===");

        // var manualPartialDecryptions = new Dictionary<int, BigInteger>
        // {
        //     [1] = new BigInteger("306597114889731876393719256055254870083758933742728341848103408230841603167919379156562067432367360092602086705633325463561612322487408019716932974070212325119600277190570892790653168466945268906796363909366029120065382662357397293865947033112959441276719467062840337342122792475967733297992145575508843393857977"), // Server 1 partial decryption
        //     [2] = new BigInteger("131744714596378644775564928368581728459431907908423135809374718470994533157536167982517577894425547899636005812661163200926573056629915756231887875298540948302586692736792018265684585090577083493971179132629778218475554742045481153005122996559496375437676267841265209763013994282630566667330942556839043086229308"), // Server 2 partial decryption
        //     [3] = new BigInteger("180711661374493869714704807847138850582250234869277269367215618530022576340680891301726966290396600280619828462527735409782142050354774676537907037862836039301949533684930521161714629373463557522739535370597154529709317244898690942603311647196818069877419588979525716941058703942016098337127939509241386360668147"), // Server 3 partial decryption
        //     [4] = new BigInteger("372890925166487389347160638116362906405370642086750573115053327926328910126048180445536812412034078401041411730092834164122755328231177283729549993863271152606139543519476672720129358714115522960898892196232638479211474465969542133844042175872462060397538682635615915559777752475901799021907003037441251856175080"), // Server 4 partial decryption
        //     [5] = new BigInteger("460379807289947835020310476470064614962486317062514398465352177442992325455541065473875023508664730882345011840860569770663110803980892451887915024180003039011110301994248128431498016578591771834393539387039521575531807298338226316909990269078949677715448532010768305823005575866885356555393737397001971925274296"), // Server 5 partial decryption
        //     [6] = new BigInteger("231414877489957875527458491573931249311524142602412307915700218723709491727121485252318544369489605615275768152530308538917143149334728521311556605849442042609505150467519676701870268089757844162367671147455501100628650798408329957273817962560133967825850183752059365199317181203981376884241509661675401069980666"), // Server 6 partial decryption
        //     [7] = new BigInteger("131873186339317937700698883499685200517542052141063327522313425550529003417124123263656746289924334321704088077590229594206916755331920743020297034249508363907740262915874545587065151120241610107266564363092218404481709142922974869120792399000831327820664334249586063384716861490323414413988919494837753725495986"), // Server 7 partial decryption
        //     [8] = new BigInteger("304858297181034217761486010391045667147044268541545179333912415999326816116164253535103179108258809139693715056715976188021086527631761173238688760840260623337044310057293266233811188837278770149796122597082992284185971675660212140560697432435502508376558235922590635210155373231632353846731197858577439205251381"), // Server 8 partial decryption
        //     [9] = new BigInteger("260687742235794464868287249878198254558707156543133528885714997123131864426176800244911191593981466963959752876387022145483751861508051798615638108392172200252185887248495644693132888216037961249694620592430660143131239594561177995647651157984503645760130449113529506018653226665694658325895032869986048358703719"), // Server 9 partial decryption
        // };

        // // join partial decryptions
        // var manualDecrypted = decryptKey.decrypt(manualPartialDecryptions);
        // Console.WriteLine($"Odszyfrowane z ręcznych partial decryptions: {manualDecrypted}");

        // var manualDecoded = Decode(manualDecrypted);
        // Console.WriteLine($"Zdekodowane: {manualDecoded}");
        // Console.WriteLine("===========================================\n");

        string Decode(BigInteger encoded)
        {
            if (encoded.Equals(BigInteger.Zero))
            {
                var a0 = _alphabet[0];
                return a0.ToString();
            }

            int baseValue = _baseValue;
            var baseValueBig = new BigInteger(baseValue.ToString());
            var result = new StringBuilder();

            while (encoded.CompareTo(BigInteger.Zero) > 0)
            {
                BigInteger remainder = encoded.Mod(baseValueBig);
                int index = int.Parse(remainder.ToString());
                result.Insert(0, _alphabet[index]);
                encoded = encoded.Divide(baseValueBig);
            }

            return result.ToString();
        }

        // using var sha256 = SHA256.Create();

        // var input = Encoding.UTF8.GetBytes($"aa");
        // byte[] hash = sha256.ComputeHash(input);
        // string hashedValue = Convert.ToHexString(hash).ToLower();

        // var c_hash = publicKey.EncryptHash(hashedValue);

        // Console.WriteLine($"hash: {hashedValue}");
        // Console.WriteLine($"c_hash: {c_hash}");

        // partialDecryptions[1] = sharedKey1.partial_decrypt(c_hash);
        // partialDecryptions[2] = sharedKey2.partial_decrypt(c_hash);
        // partialDecryptions[3] = sharedKey3.partial_decrypt(c_hash);
        // partialDecryptions[4] = sharedKey4.partial_decrypt(c_hash);
        // partialDecryptions[5] = sharedKey5.partial_decrypt(c_hash);

        // var decrypted = sharedKey1.decrypt(partialDecryptions);

        // Console.WriteLine($"Odszyfrowany hash: {publicKey.BigIntegerToHash(decrypted)}");



        // Console.WriteLine("=== ELGAMAL - MULTI SERVER TEST ===\n");

        // // Ścieżka do foldera z kluczami
        // string keyFolder = "../elGamalKeys";

        // // 1. Wygeneruj klucze dla 3 serwerów (tylko dla testu)
        // Console.WriteLine("1. Generowanie kluczy dla 10 serwerów...");
        // ElGamalEncryption.GenerateAndSaveServerKeys(numberOfServers: 10, folderPath: keyFolder, strength: 512);
        // Console.WriteLine();

        // // 2. Każdy serwer załaduje swoje klucze
        // Console.WriteLine("2. Ładowanie kluczy przez każdy serwer...");
        // var server1 = new ElGamalEncryption(serverId: 1, folderPath: keyFolder);
        // var server2 = new ElGamalEncryption(serverId: 2, folderPath: keyFolder);
        // var server3 = new ElGamalEncryption(serverId: 3, folderPath: keyFolder);
        // Console.WriteLine();

        // // 3. Serwer 1 szyfruje wiadomość dla serwera 2
        // Console.WriteLine("3. Serwer 1 szyfruje wiadomość dla serwera 2...");
        // long testMessage = 42;
        // var (c1_s1_to_s2, c2_s1_to_s2) = server1.Encrypt(testMessage, targetServerId: 2);
        // Console.WriteLine($"   Wiadomość: {testMessage}");
        // Console.WriteLine($"   Ciphertext c1: {c1_s1_to_s2}");
        // Console.WriteLine($"   Ciphertext c2: {c2_s1_to_s2}");
        // Console.WriteLine();

        // // 4. Serwer 2 re-randomizuje i deszyfruje wiadomość
        // Console.WriteLine("4. Serwer 2 re-randomizuje i deszyfruje wiadomość...");
        // var (c1_s1_to_s2_r, c2_s1_to_s2_r) = server2.ReEncrypt((c1_s1_to_s2, c2_s1_to_s2));
        // var decrypted_s2 = server2.Decrypt((c1_s1_to_s2_r, c2_s1_to_s2_r));
        // Console.WriteLine($"   Odszyfrowano: {decrypted_s2}");
        // Console.WriteLine($"   Poprawnie: {decrypted_s2.Equals(new BigInteger(testMessage.ToString()))}\n");

        // // 5. Testuj operacje homomorficzne między serwerami
        // Console.WriteLine("5. Operacje homomorficzne (Serwer 1)...");
        // long msg1 = 10;
        // long msg2 = 5;
        // var (c1_msg1, c2_msg1) = server1.Encrypt(msg1, targetServerId: 3);
        // var (c1_msg2, c2_msg2) = server1.Encrypt(msg2, targetServerId: 3);

        // // Mnożenie szyfrogramów = mnożenie tekstu jawnego
        // var (c1_mult, c2_mult) = server1.Multiply((c1_msg1, c2_msg1), (c1_msg2, c2_msg2));
        // var (c1_mult_r, c2_mult_r) = server3.ReEncrypt((c1_mult, c2_mult));
        // var decrypted_mult = server3.Decrypt((c1_mult_r, c2_mult_r));
        // Console.WriteLine($"   {msg1} * {msg2} = {decrypted_mult} (oczekiwane: {msg1 * msg2})");

        // // Dzielenie szyfrogramów = dzielenie tekstu jawnego
        // var (c1_div, c2_div) = server1.Divide((c1_msg1, c2_msg1), (c1_msg2, c2_msg2));
        // var (c1_div_r, c2_div_r) = server3.ReEncrypt((c1_div, c2_div));
        // var decrypted_div = server3.Decrypt((c1_div_r, c2_div_r));
        // Console.WriteLine($"   {msg1} / {msg2} = {decrypted_div} (oczekiwane: {msg1 / msg2})\n");

        // // 6. Serwer 3 szyfruje dla serwera 1
        // Console.WriteLine("6. Serwer 3 szyfruje wiadomość dla serwera 1...");
        // long testMessage3 = 99;
        // var (c1_s3_to_s1, c2_s3_to_s1) = server3.Encrypt(testMessage3, targetServerId: 1);
        // var (c1_s3_to_s1_r, c2_s3_to_s1_r) = server1.ReEncrypt((c1_s3_to_s1, c2_s3_to_s1));
        // var decrypted_s1 = server1.Decrypt((c1_s3_to_s1_r, c2_s3_to_s1_r));
        // Console.WriteLine($"   Wiadomość: {testMessage3}");
        // Console.WriteLine($"   Odszyfrowano przez Serwer 1: {decrypted_s1}");
        // Console.WriteLine($"   Poprawnie: {decrypted_s1.Equals(new BigInteger(testMessage3.ToString()))}\n");

        // Console.WriteLine("=== TESTY ZAKONCZONE ===");

        // Wydajność ===============================
        // var publicKey = new PaillierPublicKey();
        // var publicKeyFast = new FastPaillier();

        // for (int i = 0; i < 100; i++)
        // {
        //     publicKey.Encrypt(new BigInteger("12345678901234567890"));
        // }

        // Console.WriteLine("\n\nSzybki======================================================================================");
        // for (int i = 0; i < 100; i++)
        // {
        //     var message = publicKeyFast.RandomBigInteger(1, 12345678901234567890);
        //     publicKeyFast.Encrypt(message);
        // }


        // var swTotal = System.Diagnostics.Stopwatch.StartNew();
        // var sw = System.Diagnostics.Stopwatch.StartNew();


        // publicKeyFast.Test(100);

        // sw.Stop();
        // long time = sw.ElapsedMilliseconds;
        // Console.WriteLine($"\n\n{time}");

        // sw.Restart();

        // publicKey.Test(100);

        // sw.Stop();
        // time = sw.ElapsedMilliseconds;
        // Console.WriteLine($"\n\n{time}");

        ///////////////////////////////////////

        // var publicKey = new PaillierPublicKey();


        // var swTotal = System.Diagnostics.Stopwatch.StartNew();
        // var sw = System.Diagnostics.Stopwatch.StartNew();

        // Parallel.For(0, 30000, i =>
        // {
        //     var encrypted = publicKey.Encrypt(new BigInteger("12345678901234567890"));
        // });

        // sw.Stop();
        // long time1 = sw.ElapsedMilliseconds;
        // Console.WriteLine($"\n\n{time1}");

        // sw.Restart();

        // for (int i = 0; i < 30000; i++)
        // {
        //     var encrypted = publicKey.Encrypt(new BigInteger("12345678901234567890"));
        // }

        // sw.Stop();
        // long time2 = sw.ElapsedMilliseconds;
        // Console.WriteLine($"\n\n{time1}");
        // Console.WriteLine($"\n\n{time2}");
    }
}
