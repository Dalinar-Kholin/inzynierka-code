package obliviousTransfer

import (
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"encoding/hex"
	"errors"
	"golangShared"
	"math/big"

	"golang.org/x/crypto/hkdf"
)

var pHex = "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD129024E08" +
	"8A67CC74020BBEA63B139B22514A08798E3404DDEF9519B3CD" +
	"3A431B302B0A6DF25F14374FE1356D6D51C245E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7ED" +
	"EE386BFB5A899FA5AE9F24117C4B1FE649286651ECE65381FFFFFFFFFFFFFFFF"

var (
	p = new(big.Int)
	g = big.NewInt(2)
)

func FindUnused(auth *golangShared.AuthPackage) (string, error) {
	for i := range auth.AuthCode {
		if !auth.AuthCode[i].IsScratched {
			auth.AuthCode[i].IsScratched = true
			return base64.StdEncoding.EncodeToString(auth.AuthCode[i].Code[:]), nil
		}
	}
	return "", errors.New("no unused auth code")
}

func init() {
	b, _ := hex.DecodeString(pHex)
	p.SetBytes(b)
}

type InitOutput struct {
	N string `json:"n"`
	G string `json:"g"`
	C string `json:"C"`
}

type EncryptIn struct {
	A string `json:"A"`
	B string `json:"B"`
}

func randZq() *big.Int {
	// sample in [2, p-2]
	for {
		x, _ := rand.Int(rand.Reader, new(big.Int).Sub(p, big.NewInt(3)))
		x.Add(x, big.NewInt(2))
		if x.Sign() > 0 {
			return x
		}
	}
}

func modExp(a, e, mod *big.Int) *big.Int { return new(big.Int).Exp(a, e, mod) }
func mulMod(a, b, mod *big.Int) *big.Int { return new(big.Int).Mod(new(big.Int).Mul(a, b), mod) }

func padTo(bytesLen int, b []byte) []byte {
	if len(b) >= bytesLen {
		return b
	}
	out := make([]byte, bytesLen)
	copy(out[len(out)-len(b):], b)
	return out
}

func kdfKey(z *big.Int, info []byte, outLen int) []byte {
	zBytes := padTo(len(p.Bytes()), z.Bytes())
	h := hkdf.New(sha256.New, zBytes, nil, info)
	key := make([]byte, outLen)
	h.Read(key)
	return key
}

func BigIntToString(x *big.Int) string { return x.String() }

func StringToBigInt(s string) (*big.Int, bool) {
	z := new(big.Int)
	_, ok := z.SetString(s, 10)
	return z, ok
}
