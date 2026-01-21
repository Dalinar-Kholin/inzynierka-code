package helpers

import (
	"crypto/sha512"
	"encoding/binary"
	"math/big"

	"github.com/enceve/crypto/dh"
)

var grp = dh.RFC3526_2048() // p,g z RFC 3526, 2048-bit

var p = grp.P
var g = grp.G
var q = big.NewInt(0).Div(big.NewInt(0).Sub(p, big.NewInt(1)), big.NewInt(2)) // == (p - 1) /2

func Commit(m, r string) *big.Int {
	M := HashToScalar([]byte("m:" + m))
	R := HashToScalar([]byte("r:" + r))

	gm := new(big.Int).Exp(g, M, p)
	hr := new(big.Int).Exp(deriveH(), R, p)

	C := new(big.Int).Mul(gm, hr)
	C.Mod(C, p)
	return C
}

func Unpack(m, r string, C *big.Int) bool {
	M := HashToScalar([]byte("m:" + m))
	R := HashToScalar([]byte("r:" + r))

	gm := new(big.Int).Exp(g, M, p)
	hr := new(big.Int).Exp(deriveH(), R, p)

	calcedC := new(big.Int).Mul(gm, hr)
	calcedC.Mod(calcedC, p)
	return calcedC.Cmp(C) == 0
}

func deriveH() *big.Int {
	one := big.NewInt(1)
	pMinusOneDivQ := new(big.Int).Div(new(big.Int).Sub(p, one), q) // (p-1)/q

	var counter uint64
	for {
		sha := sha512.New()
		sha.Write([]byte("pedersen-h-generator"))
		sha.Write(p.Bytes())
		sha.Write(g.Bytes())

		var ctrBuf [8]byte
		binary.BigEndian.PutUint64(ctrBuf[:], counter)
		sha.Write(ctrBuf[:])

		sum := sha.Sum(nil)

		// 1. Hash -> losowy element [2, p-2]
		t := new(big.Int).SetBytes(sum)
		t.Mod(t, new(big.Int).Sub(p, one)) // t \in [0, p-2]
		t.Add(t, one)                      // t \in [1, p-1]

		// 2. Rzutowanie do podgrupy rzędu q
		// h = t^((p-1)/q) mod p
		// To jest standardowa metoda w Zp* na wylosowanie elementu z podgrupy
		h := new(big.Int).Exp(t, pMinusOneDivQ, p)

		// 3. Sprawdź, czy nie jest to element neutralny (tj. 1)
		if h.Cmp(one) != 0 {
			return h
		}

		counter++
	}
}

func HashToScalar(data []byte) *big.Int {
	sum := sha512.Sum512(data)
	x := new(big.Int).SetBytes(sum[:])
	x.Mod(x, q)
	if x.Sign() == 0 {
		x.SetInt64(1) // avoid zero exponent
	}
	return x
}
