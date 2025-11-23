package helper

import (
	"crypto/ed25519"
)

var SignKey ed25519.PrivateKey = nil

func Sign(message []byte) []byte {
	signature := ed25519.Sign(SignKey, message)
	return signature
}
