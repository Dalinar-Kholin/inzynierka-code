package helpers

import (
	"crypto/rand"
	"golangShared"
	"math/big"
)

func SecureRandomString() [golangShared.AuthCodeLength]byte {
	var out [golangShared.AuthCodeLength]byte
	for x := range out {
		out[x] = charset[cryptoRandInt(len(charset))]
	}

	return out
}

const charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"

func cryptoRandInt(n int) int {
	if n <= 0 {
		return 0
	}
	max := big.NewInt(int64(n))
	v, err := rand.Int(rand.Reader, max)
	if err != nil {
		panic(err)
	}
	return int(v.Int64())
}
