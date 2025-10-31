package common

import (
	"bytes"
	"crypto/aes"
	"crypto/cipher"
	"crypto/ecdh"
	"crypto/rand"
	"crypto/sha256"
	"encoding/base64"
	"encoding/binary"
	"errors"
	"fmt"
	"golang.org/x/crypto/hkdf"
	"io"
)

// ICOM: na razie unused

const (
	versionByte    = 0x01
	suiteX25519GCM = 0xA1 // arbitralny identyfikator zestawu kryptograficznego

	x25519PubLen = 32
	saltLen      = 16
	nonceLen     = 12 // AES-GCM standard
)

func deriveKeyHKDF(sharedSecret, salt, info []byte, outLen int) ([]byte, error) {
	kdf := hkdf.New(sha256.New, sharedSecret, salt, info)
	key := make([]byte, outLen)
	if _, err := io.ReadFull(kdf, key); err != nil {
		return nil, err
	}
	return key, nil
}

func sealAESGCM(key, nonce, plaintext, aad []byte) ([]byte, error) {
	block, err := aes.NewCipher(key)
	if err != nil {
		return nil, err
	}
	aead, err := cipher.NewGCM(block)
	if err != nil {
		return nil, err
	}
	return aead.Seal(nil, nonce, plaintext, aad), nil
}

func openAESGCM(key, nonce, ciphertext, aad []byte) ([]byte, error) {
	block, err := aes.NewCipher(key)
	if err != nil {
		return nil, err
	}
	aead, err := cipher.NewGCM(block)
	if err != nil {
		return nil, err
	}
	return aead.Open(nil, nonce, ciphertext, aad)
}

func EncryptECIES(receiverPub *ecdh.PublicKey, plaintext, aad []byte) (*string, error) {
	if receiverPub == nil {
		return nil, errors.New("receiver public key is nil")
	}
	curve := ecdh.X25519()

	// 1) Ephemeral keypair
	ephemPriv, err := curve.GenerateKey(rand.Reader)
	if err != nil {
		return nil, fmt.Errorf("ephemeral key gen: %w", err)
	}
	ephemPub := ephemPriv.PublicKey().Bytes()
	if len(ephemPub) != x25519PubLen {
		return nil, errors.New("unexpected ephem pub len")
	}

	// 2) ECDH shared secret
	shared, err := ephemPriv.ECDH(receiverPub)
	if err != nil {
		return nil, fmt.Errorf("ecdh: %w", err)
	}

	// 3) HKDF -> klucz AES-256
	salt := make([]byte, saltLen)
	if _, err := rand.Read(salt); err != nil {
		return nil, fmt.Errorf("salt: %w", err)
	}
	info := []byte("ECIES-X25519-AESGCM-v1")
	key, err := deriveKeyHKDF(shared, salt, info, 32)
	if err != nil {
		return nil, fmt.Errorf("hkdf: %w", err)
	}

	// 4) AES-GCM
	nonce := make([]byte, nonceLen)
	if _, err := rand.Read(nonce); err != nil {
		return nil, fmt.Errorf("nonce: %w", err)
	}
	ciphertext, err := sealAESGCM(key, nonce, plaintext, aad)
	if err != nil {
		return nil, fmt.Errorf("gcm seal: %w", err)
	}

	// 5) Pakiet
	var b bytes.Buffer
	b.WriteByte(versionByte)
	b.WriteByte(suiteX25519GCM)
	b.Write(ephemPub)
	b.Write(salt)
	b.Write(nonce)
	var clen [4]byte
	binary.BigEndian.PutUint32(clen[:], uint32(len(ciphertext)))
	b.Write(clen[:])
	b.Write(ciphertext)

	sEnc := base64.StdEncoding.EncodeToString(b.Bytes())

	return &sEnc, nil
}

func DecryptECIES(receiverPriv *ecdh.PrivateKey, basedPacket *string, aad []byte) ([]byte, error) {
	packet, err := base64.StdEncoding.DecodeString(*basedPacket)

	if receiverPriv == nil || err != nil {
		return nil, errors.New("receiver private key is nil or bad message")
	}

	// Minimalna długość nagłówka
	min := 1 + 1 + x25519PubLen + saltLen + nonceLen + 4
	if len(packet) < min {
		return nil, errors.New("packet too short")
	}

	off := 0
	ver := packet[off]
	off++
	if ver != versionByte {
		return nil, fmt.Errorf("unsupported version: %d", ver)
	}
	suite := packet[off]
	off++
	if suite != suiteX25519GCM {
		return nil, fmt.Errorf("unsupported suite: 0x%X", suite)
	}

	ephemPubBytes := packet[off : off+x25519PubLen]
	off += x25519PubLen
	salt := packet[off : off+saltLen]
	off += saltLen
	nonce := packet[off : off+nonceLen]
	off += nonceLen
	clen := binary.BigEndian.Uint32(packet[off : off+4])
	off += 4
	if int(off)+int(clen) != len(packet) {
		return nil, errors.New("invalid ciphertext length")
	}
	ciphertext := packet[off:]

	// 1) Zbuduj obiekt klucza pub z bajtów
	curve := ecdh.X25519()
	ephemPub, err := curve.NewPublicKey(ephemPubBytes)
	if err != nil {
		return nil, fmt.Errorf("bad ephem pub: %w", err)
	}

	// 2) ECDH
	shared, err := receiverPriv.ECDH(ephemPub)
	if err != nil {
		return nil, fmt.Errorf("ecdh: %w", err)
	}

	// 3) HKDF -> klucz AES-256
	info := []byte("ECIES-X25519-AESGCM-v1")
	key, err := deriveKeyHKDF(shared, salt, info, 32)
	if err != nil {
		return nil, fmt.Errorf("hkdf: %w", err)
	}

	// 4) GCM open
	plaintext, err := openAESGCM(key, nonce, ciphertext, aad)
	if err != nil {
		return nil, fmt.Errorf("gcm open: %w", err)
	}
	return plaintext, nil
}
