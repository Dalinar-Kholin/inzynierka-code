#include <gmp.h>
#include <gmpxx.h>
#include <cstring>

extern "C"
{

    // base^exp mod mod -> result (stara wersja ze stringami)
    void modpow(const char *base_str, const char *exp_str, const char *mod_str,
                char *result_str, int result_size)
    {
        mpz_class base(base_str);
        mpz_class exp(exp_str);
        mpz_class mod(mod_str);
        mpz_class res;

        if (exp < 0)
        {
            mpz_invert(res.get_mpz_t(), base.get_mpz_t(), mod.get_mpz_t());
            mpz_class pos_exp = -exp;
            mpz_powm(res.get_mpz_t(), res.get_mpz_t(), pos_exp.get_mpz_t(), mod.get_mpz_t());
        }
        else
        {
            mpz_powm(res.get_mpz_t(), base.get_mpz_t(), exp.get_mpz_t(), mod.get_mpz_t());
        }

        std::string s = res.get_str(10);
        strncpy(result_str, s.c_str(), result_size - 1);
        result_str[result_size - 1] = 0;
    }

    // base^exp mod mod -> result (nowa wersja z raw bytes - dużo szybsza!)
    void modpow_bytes(const unsigned char *base_bytes, int base_len,
                      const unsigned char *exp_bytes, int exp_len,
                      const unsigned char *mod_bytes, int mod_len,
                      unsigned char *result_bytes, int *result_len)
    {
        mpz_t base, exp, mod, res;
        mpz_init(base);
        mpz_init(exp);
        mpz_init(mod);
        mpz_init(res);

        // Import bytes - little-endian (LSB first) to match C# BigInteger
        // count=base_len (number of bytes), order=-1 (LSB first), size=1 (1 byte), endian=0 (native), nails=0
        mpz_import(base, base_len, -1, 1, 0, 0, base_bytes);
        mpz_import(exp, exp_len, -1, 1, 0, 0, exp_bytes);
        mpz_import(mod, mod_len, -1, 1, 0, 0, mod_bytes);

        // Calculate base^exp mod mod
        mpz_powm(res, base, exp, mod);

        // Export result to bytes - little-endian (LSB first)
        size_t count = 0;
        mpz_export(result_bytes, &count, -1, 1, 0, 0, res);
        *result_len = (int)count;

        mpz_clear(base);
        mpz_clear(exp);
        mpz_clear(mod);
        mpz_clear(res);
    }
}
