package helper

import (
	"crypto/ed25519"
	"fmt"
)

var SignKey ed25519.PrivateKey = nil

func Sign(message []byte) []byte {
	signature := ed25519.Sign(SignKey, message)
	fmt.Printf("signature := _%v_\n", string(signature))
	return signature
}
