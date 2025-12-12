#include <gmp.h>
#include <gmpxx.h>
#include <hiredis/hiredis.h>
#include <iostream>
#include <random>
#include <string>
#include <fstream>
#include <thread>
#include <vector>
#include <nlohmann/json.hpp>

using namespace std;
using json = nlohmann::json;

mpz_class random_bigint(const mpz_class &n, mt19937_64 &rng)
{
    size_t bytes = (mpz_sizeinbase(n.get_mpz_t(), 2) + 7) / 8;
    mpz_class r;
    while (true)
    {
        string buf(bytes, 0);
        for (size_t i = 0; i < bytes; i++)
            buf[i] = rng() & 0xFF;

        mpz_import(r.get_mpz_t(), bytes, 1, 1, 0, 0, buf.data());
        if (r > 0 && r < n)
            return r;
    }
}

void worker_thread(const mpz_class &n, const mpz_class &n_squared,
                   const char *redis_host, int redis_port,
                   const char *redis_queue_name,
                   int thread_id, int batch_size)
{
    // każde połączenie Redis własne
    redisContext *redis = redisConnect(redis_host, redis_port);
    if (!redis || redis->err)
    {
        cerr << "[Thread " << thread_id << "] Redis error: "
             << (redis ? redis->errstr : "connection failed") << endl;
        return;
    }

    mt19937_64 rng(random_device{}() + thread_id);
    vector<string> batch;
    batch.reserve(batch_size);
    long counter = 0;
    int log_count = 0;

    while (true)
    {
        mpz_class r = random_bigint(n, rng);
        mpz_class rn;
        mpz_powm(rn.get_mpz_t(), r.get_mpz_t(), n.get_mpz_t(), n_squared.get_mpz_t());

        string rn_str = rn.get_str(10);
        batch.push_back(rn_str);

        // Wypisz pierwsze 10 logów
        if (log_count < 10)
        {
            cout << "[Thread " << thread_id << " - Log " << (log_count + 1) << "/10]" << endl;
            cout << "  r = " << r.get_str(10) << endl;
            cout << "  n = " << n.get_str(10) << endl;
            cout << "  r^n mod n^2 = " << rn_str << endl;
            cout.flush();
            log_count++;
        }

        if (batch.size() >= batch_size)
        {
            // przygotowanie LPUSH batch
            string cmd = "LPUSH ";
            cmd += redis_queue_name;
            for (auto &s : batch)
                cmd += " " + s;

            redisReply *reply = (redisReply *)redisCommand(redis, cmd.c_str());
            if (!reply)
            {
                cerr << "[Thread " << thread_id << "] Redis command failed" << endl;
                batch.clear();
                continue;
            }
            freeReplyObject(reply);

            counter += batch.size();
            cout << "[Thread " << thread_id << "] Generated " << counter << " values" << endl;
            cout.flush();

            batch.clear();
        }
    }

    redisFree(redis);
}

int main()
{
    const int NUM_THREADS = 14;
    const int BATCH_SIZE = 100; // możesz zwiększyć dla większej wydajności
    const char *redis_host = "127.0.0.1";
    int redis_port = 6379;
    const char *redis_queue_name = "paillier_rn_fifo";

    // wczytaj n i n_squared z JSON
    mpz_class n, n_squared;
    ifstream f("../../../encryption/paillierKeys/paillier_keys_public.json");
    if (!f.is_open())
    {
        cerr << "Cannot open key file";
        return 1;
    }
    try
    {
        json keys_json;
        f >> keys_json;
        n = mpz_class(keys_json["n"].get<string>());
        n_squared = mpz_class(keys_json["n_squared"].get<string>());
        cout << "[INFO] Loaded keys successfully" << endl;
    }
    catch (const exception &e)
    {
        cerr << "Cannot parse JSON: " << e.what() << endl;
        return 1;
    }

    cout << "[START] Generating r^n mod n^2 with " << NUM_THREADS << " threads..." << endl;

    vector<thread> threads;
    for (int i = 0; i < NUM_THREADS; i++)
        threads.emplace_back(worker_thread, cref(n), cref(n_squared),
                             redis_host, redis_port, redis_queue_name,
                             i, BATCH_SIZE);

    for (auto &t : threads)
        t.join();

    return 0;
}
