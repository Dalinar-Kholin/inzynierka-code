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
	"net/http"
	"votingServer/DB"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"
)

type EncryptData struct {
	A          string `json:"a"`
	B          string `json:"b"`
	AuthSerial string `json:"authSerial"`
}

func Encrypt(context *gin.Context) {

	var body EncryptData
	if err := context.ShouldBindJSON(&body); err != nil {
		panic(err)
	}

	var otid ObliviousTransferInitData
	authSerial, err := uuid.Parse(body.AuthSerial)
	if err != nil {
		panic(err)
	}
	if err := DB.GetDataBase("inz", DB.ObliviousTransferInit).FindOne(
		context2.Background(),
		bson.M{
			"$and": []bson.M{
				{"authSerial": primitive.Binary{
					Subtype: 0x04,          // UUID standard
					Data:    authSerial[:], // 16 bajtów
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
			Subtype: 0x04,          // UUID standard
			Data:    authSerial[:], // 16 bajtów
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

	c, ok := StringToBigInt(otid.C)
	if !ok {
		panic("invalid otid")
	}

	Abytes, _ := hex.DecodeString(body.A)
	Bbytes, _ := hex.DecodeString(body.B)
	A := new(big.Int).SetBytes(Abytes)
	B := new(big.Int).SetBytes(Bbytes)

	CValue := modExp(g, c, p)
	if mulMod(A, B, p).Cmp(CValue) != 0 {
		panic(fmt.Sprintf("invalid A * B != C %v", c))
	}

	s0 := randZq()
	s1 := randZq()

	X0 := modExp(g, s0, p)
	X1 := modExp(g, s1, p)

	Z0 := modExp(A, s0, p)
	Z1 := modExp(B, s1, p)

	info := []byte("p:" + pHex + "|g:02|A:" + body.A + "|B:" + body.B + "|X0:" + hex.EncodeToString(X0.Bytes()) + "|X1:" + hex.EncodeToString(X1.Bytes()))

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
	context.JSON(http.StatusOK, gin.H{
		"X0": hex.EncodeToString(X0.Bytes()),
		"X1": hex.EncodeToString(X1.Bytes()),
		"n0": hex.EncodeToString(n0),
		"n1": hex.EncodeToString(n1),
		"c0": hex.EncodeToString(c0),
		"c1": hex.EncodeToString(c1),
	})
}
