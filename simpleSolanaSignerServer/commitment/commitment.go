package commitment

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

func CommitVotePack() {
	//TODO implement me
	panic("implement me")
}

func CommitAuthPack(authSerial, data []byte) {
	var out [16]byte
	copy(out[:], authSerial)

	var dataCopied [32]byte
	copy(dataCopied[:], data)

	_, err := CallCreateCommitmentPack(out, dataCopied)
	if err != nil {
		panic(err)
	}
}

func Commit(id, data string) {
	sum := sha512.New().Sum([]byte(data))
	fmt.Printf("id := %s, sha := %s\n", id, sum)
}
