package obliviousTransfer

import (
	context2 "context"
	"crypto/aes"
	"crypto/cipher"
	"crypto/rand"
	"encoding/hex"
	"errors"
	"fmt"
	"golangShared"
	"math/big"
	"votingServer/DB"

	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"
)

type UserResponse struct {
	A          string `json:"a"`
	B          string `json:"b"`
	AuthSerial string `json:"authSerial"`
}

type EncryptResponse struct {
	X0 string `json:"x0"`
	X1 string `json:"x1"`
	N0 string `json:"n0"`
	N1 string `json:"n1"`
	C0 string `json:"c0"`
	C1 string `json:"c1"`
}

func Encrypt(userResponse *UserResponse) *EncryptResponse {
	var otid ObliviousTransferInitData
	authSerial, err := uuid.Parse(userResponse.AuthSerial)
	if err != nil {
		panic(err)
	}
	if err := DB.GetDataBase("inz", DB.ObliviousTransferInit).FindOne(
		context2.Background(),
		bson.M{
			"$and": []bson.M{
				{"authSerial": primitive.Binary{
					Subtype: 0x04,
					Data:    authSerial[:],
				}}, {
					"used": false,
				},
			},
		},
	).Decode(&otid); err != nil {
		panic(err)
	}

	var Auth golangShared.AuthPackage
	if err := DB.GetDataBase("inz", DB.AuthCollection).FindOne(
		context2.Background(),
		bson.M{"authSerial": primitive.Binary{
			Subtype: 0x04,
			Data:    authSerial[:],
		}},
	).Decode(&Auth); errors.Is(err, mongo.ErrNoDocuments) {
		panic(err)
	}

	ac1, err := FindUnused(&Auth)
	if err != nil {
		panic(err)
	}
	ac2, err := FindUnused(&Auth)
	if err != nil {
		panic(err)
	}

	if _, err := DB.GetDataBase("inz", DB.AuthCollection).ReplaceOne(
		context2.Background(),
		bson.M{"authSerial": primitive.Binary{
			Subtype: 0x04,
			Data:    authSerial[:],
		}},
		Auth,
	); err != nil {
		panic(err)
	}

	c, ok := StringToBigInt(otid.C)
	if !ok {
		panic("invalid otid")
	}

	Abytes, _ := hex.DecodeString(userResponse.A)
	Bbytes, _ := hex.DecodeString(userResponse.B)

	A := new(big.Int).SetBytes(Abytes)
	B := new(big.Int).SetBytes(Bbytes)

	CValue := modExp(g, c, p)
	if mulMod(A, B, p).Cmp(CValue) != 0 {
		panic(fmt.Sprintf("invalid A * B != C %v", c))
	}

	s0 := randZq() // server secrets
	s1 := randZq()

	X0 := modExp(g, s0, p) // server pub key
	X1 := modExp(g, s1, p)

	Z0 := modExp(A, s0, p) // shared secret
	Z1 := modExp(B, s1, p)

	info := []byte("p:" + pHex + "|g:02|A:" + userResponse.A + "|B:" + userResponse.B + "|X0:" + hex.EncodeToString(X0.Bytes()) + "|X1:" + hex.EncodeToString(X1.Bytes()))

	k0 := kdfKey(Z0, info, 32)
	k1 := kdfKey(Z1, info, 32)

	b0, _ := aes.NewCipher(k0)
	aead0, _ := cipher.NewGCM(b0)
	n0 := make([]byte, aead0.NonceSize())
	rand.Read(n0)
	c0 := aead0.Seal(nil, n0, []byte(ac1), info)
	fmt.Printf("%v\n", ac1)

	b1, _ := aes.NewCipher(k1)
	aead1, _ := cipher.NewGCM(b1)
	n1 := make([]byte, aead1.NonceSize())
	rand.Read(n1)
	c1 := aead1.Seal(nil, n1, []byte(ac2), info)
	return &EncryptResponse{
		hex.EncodeToString(X0.Bytes()),
		hex.EncodeToString(X1.Bytes()),
		hex.EncodeToString(n0),
		hex.EncodeToString(n1),
		hex.EncodeToString(c0),
		hex.EncodeToString(c1),
	}
}
