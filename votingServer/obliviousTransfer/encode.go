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

func Encrypt(userResponse *UserResponse) (*EncryptResponse, error) {
	authSerial, err := uuid.Parse(userResponse.AuthSerial)
	if err != nil {
		return nil, err
	}

	var Auth golangShared.AuthPackage
	if err := DB.GetDataBase("inz", DB.AuthCollection).FindOne(
		context2.Background(),
		bson.M{"authSerial": primitive.Binary{
			Subtype: 0x04,
			Data:    authSerial[:],
		}},
	).Decode(&Auth); errors.Is(err, mongo.ErrNoDocuments) {
		return nil, err
	}

	authPack, err := FindActual(&Auth)
	if err != nil {
		return nil, err
	}

	c := HexToBigInt(authPack.C)

	A := HexToBigInt(userResponse.A)
	B := HexToBigInt(userResponse.B)

	CValue := modExp(g, c, p)
	if mulMod(A, B, p).Cmp(CValue) != 0 {
		return nil, errors.New(fmt.Sprintf("invalid A * B != C\nC:= %v\nA:= %v\nB:= %v\n", c, A, B))
	}

	s0 := RandZq() // server secrets
	s1 := RandZq()

	X0 := modExp(g, s0, p) // server pub key
	X1 := modExp(g, s1, p)

	Z0 := modExp(A, s0, p) // shared secret
	Z1 := modExp(B, s1, p)

	info := []byte("p:" + PHex + "|g:02|A:" + userResponse.A + "|B:" + userResponse.B + "|X0:" + hex.EncodeToString(X0.Bytes()) + "|X1:" + hex.EncodeToString(X1.Bytes()))

	k0 := kdfKey(Z0, info, 32)
	k1 := kdfKey(Z1, info, 32)

	b0, err := aes.NewCipher(k0)
	if err != nil {
		return nil, err
	}
	aead0, err := cipher.NewGCM(b0)
	if err != nil {
		return nil, err
	}
	n0 := make([]byte, aead0.NonceSize())
	rand.Read(n0)
	c0 := aead0.Seal(nil, n0, authPack.Code[0].Data, info)

	b1, err := aes.NewCipher(k1)
	if err != nil {
		return nil, err
	}
	aead1, err := cipher.NewGCM(b1)
	if err != nil {
		return nil, err
	}
	n1 := make([]byte, aead1.NonceSize())
	rand.Read(n1)
	c1 := aead1.Seal(nil, n1, authPack.Code[1].Data, info)
	return &EncryptResponse{
		hex.EncodeToString(X0.Bytes()),
		hex.EncodeToString(X1.Bytes()),
		hex.EncodeToString(n0),
		hex.EncodeToString(n1),
		hex.EncodeToString(c0),
		hex.EncodeToString(c1),
	}, nil
}
