package signer

import (
	"crypto/ed25519"
	"crypto/x509"
	"encoding/pem"
	"fmt"
	"os"
)

var SignKey ed25519.PrivateKey = nil

func Sign(message []byte) []byte {
	signature := ed25519.Sign(SignKey, message)
	return signature
}

func Verify(message []byte, signature []byte) bool {
	publicKey := SignKey.Public().(ed25519.PublicKey)
	return ed25519.Verify(publicKey, message, signature)
}

func LoadEd25519PrivateKey(path string) (ed25519.PrivateKey, error) {
	pemBytes, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("read file: %w", err)
	}

	block, _ := pem.Decode(pemBytes)
	if block == nil {
		return nil, fmt.Errorf("no PEM block found")
	}
	if block.Type != "PRIVATE KEY" {
		return nil, fmt.Errorf("unexpected PEM type: %s", block.Type)
	}

	// OpenSSL genpkey ED25519 → PKCS#8
	parsedKey, err := x509.ParsePKCS8PrivateKey(block.Bytes)
	if err != nil {
		return nil, fmt.Errorf("parse PKCS#8: %w", err)
	}

	priv, ok := parsedKey.(ed25519.PrivateKey)
	if !ok {
		return nil, fmt.Errorf("not an Ed25519 private key")
	}

	return priv, nil
}
