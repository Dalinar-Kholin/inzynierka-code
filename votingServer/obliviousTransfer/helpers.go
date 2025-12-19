package obliviousTransfer

import (
	"crypto/rand"
	"crypto/sha256"
	"encoding/hex"
	"errors"
	"golangShared"
	"math/big"
	"strings"

	"golang.org/x/crypto/hkdf"
)

var PHex = "FFFFFFFFFFFFFFFFADF85458A2BB4A9AAFDC5620273D3CF1D8B9C583CE2D3695A9E13641146433FBCC939DCE249B3EF97D2FE363630C75D8F681B202AEC4617AD3DF1ED5D5FD65612433F51F5F066ED0856365553DED1AF3B557135E7F57C935984F0C70E0E68B77E2A689DAF3EFE8721DF158A136ADE73530ACCA4F483A797ABC0AB182B324FB61D108A94BB2C8E3FBB96ADAB760D7F4681D4F42A3DE394DF4AE56EDE76372BB190B07A7C8EE0A6D709E02FCE1CDF7E2ECC03404CD28342F619172FE9CE98583FF8E4F1232EEF28183C3FE3B1B4C6FAD733BB5FCBC2EC22005C58EF1837D1683B2C6F34A26C1B2EFFA886B4238611FCFDCDE355B3B6519035BBC34F4DEF99C023861B46FC9D6E6C9077AD91D2691F7F7EE598CB0FAC186D91CAEFE130985139270B4130C93BC437944F4FD4452E2D74DD364F2E21E71F54BFF5CAE82AB9C9DF69EE86D2BC522363A0DABC521979B0DEADA1DBF9A42D5C4484E0ABCD06BFA53DDFE9F1F7E32F8F2D4F7E3C44E2E94B8C8E9F0A0FFFFFFFFFFFFFFFFFFFF"

var (
	p = new(big.Int)
	g = big.NewInt(2)
)

func FindUnused(auth *golangShared.AuthPackage) (*golangShared.AuthCodePack, error) {
	for i := range auth.AuthCode {
		if auth.AuthCode[i].Status == golangShared.UNUSED {
			auth.AuthCode[i].Status = golangShared.ACTUAL
			return &auth.AuthCode[i], nil
		}
	}
	return nil, errors.New("no unused auth code")
}

func FindActual(auth *golangShared.AuthPackage) (*golangShared.AuthCodePack, error) {
	for i := range auth.AuthCode {
		if auth.AuthCode[i].Status == golangShared.ACTUAL {
			return &auth.AuthCode[i], nil
		}
	}
	return nil, errors.New("no actual auth code")
}

func init() {
	b, _ := hex.DecodeString(PHex)
	p.SetBytes(b)
}

type InitOutput struct {
	N string `json:"n"`
	G string `json:"g"`
	C string `json:"c"`
}

type EncryptIn struct {
	A string `json:"a"`
	B string `json:"b"`
}

func RandZq() *big.Int {
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

func HexToBigInt(s string) *big.Int {
	h := strings.TrimPrefix(s, "0x")
	// jeżeli nieparzysta długość – dopełnij z lewej
	if len(h)%2 == 1 {
		h = "0" + h
	}
	b, err := hex.DecodeString(h)
	if err != nil {
		panic(err)
	}
	z := new(big.Int)
	z.SetBytes(b)
	return z
}
