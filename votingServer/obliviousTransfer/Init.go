package obliviousTransfer

import (
	context2 "context"
	"encoding/base64"
	"encoding/hex"
	"errors"
	"fmt"
	"golangShared"
	"net/http"
	"votingServer/DB"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"
	"golang.org/x/net/context"
)

type InitOT struct {
	AuthSerial string `json:"authSerial"`
}

func FindUnused(auth *golangShared.AuthPackage) (string, error) {
	coll := DB.GetDataBase("inz", DB.AuthCollection)

	for i := range auth.AuthCode {
		if !auth.AuthCode[i].IsScratched {
			auth.AuthCode[i].IsScratched = true

			filter := bson.M{
				"authSerial": primitive.Binary{
					Subtype: 0x04,
					Data:    auth.AuthSerial.Data, // 16 bajtów
				},
			}
			if _, err := coll.ReplaceOne(context.Background(), filter, auth); err != nil {
				return "", err
			}
			return base64.StdEncoding.EncodeToString(auth.AuthCode[i].Code[:]), nil
		}
	}
	return "", errors.New("no unused auth code")
}

func InitProtocol(context *gin.Context) {
	c := randZq()
	C := modExp(g, c, p)

	var body InitOT
	if err := context.ShouldBindJSON(&body); err != nil {
		panic(err)
	}
	fmt.Printf("data := %v\n", body)

	// zdrapujemy 2 zdrapki
	var Auth golangShared.AuthPackage
	authSerial, err := uuid.Parse(body.AuthSerial)
	if err != nil {
		panic(err)
	}

	_, err = DB.GetDataBase("inz", DB.ObliviousTransferInit).UpdateMany(
		context2.Background(),
		bson.M{"authSerial": primitive.Binary{
			Subtype: 0x04,          // UUID standard
			Data:    authSerial[:], // 16 bajtów
		}},
		bson.M{"$set": bson.M{"used": true}},
	)
	if err != nil {
		panic(err)
	}

	if err := DB.GetDataBase("inz", DB.AuthCollection).FindOne(
		context2.Background(),
		bson.M{
			"authSerial": primitive.Binary{
				Subtype: 0x04,          // UUID standard
				Data:    authSerial[:], // 16 bajtów
			},
		},
	).Decode(&Auth); errors.Is(err, mongo.ErrNoDocuments) {
		panic(err)
	}

	otid := ObliviousTransferInitData{
		primitive.Binary{
			Subtype: 0x04,          // UUID standard
			Data:    authSerial[:], // 16 bajtów
		},
		BigIntToString(c),
		false,
	}

	if _, err := DB.GetDataBase("inz", DB.ObliviousTransferInit).InsertOne(context2.Background(), otid); err != nil {
		panic(err)
	}

	data := InitOutput{N: pHex, G: "02", C: hex.EncodeToString(C.Bytes())}
	context.JSON(http.StatusOK, data)
	return
}
