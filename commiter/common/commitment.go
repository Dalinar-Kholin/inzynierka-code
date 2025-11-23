package common

import (
	"crypto/ecdh"
	"crypto/sha512"
	"fmt"
)

var publicKey *ecdh.PublicKey
var privateKey *ecdh.PrivateKey

func SetKeys(privKey *ecdh.PrivateKey, pubKey *ecdh.PublicKey) {
	publicKey = pubKey
	privateKey = privKey
}

func CommitAuthPack(commitmentType CommitmentType, data []byte) {
	var dataCopied [32]byte
	copy(dataCopied[:], data)

	_, err := CallCreateCommitmentPack(commitmentType, dataCopied)
	if err != nil {
		panic(err)
	}
}

func Commit(id, data string) {
	sum := sha512.New().Sum([]byte(data))
	fmt.Printf("id := %s, sha := %s\n", id, sum)
}
