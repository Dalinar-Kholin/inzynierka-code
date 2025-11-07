package obliviousTransfer

import (
	context2 "context"
	"encoding/hex"
	"errors"
	"golangShared"
	"votingServer/DB"

	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"
)

type InitOT struct {
	AuthSerial string `json:"authSerial"`
}

type ObliviousTransferInitData struct {
	AuthSerial primitive.Binary `bson:"authSerial"`
	C          string           `bson:"c"`
	Used       bool             `bson:"used"`
}


func InitProtocol(initOt *InitOT) *InitOutput {
	c := randZq()
	C := modExp(g, c, p)

	var Auth golangShared.AuthPackage
	authSerial, err := uuid.Parse(initOt.AuthSerial)
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
				Subtype: 0x04,
				Data:    authSerial[:],
			},
		},
	).Decode(&Auth); errors.Is(err, mongo.ErrNoDocuments) {
		panic(err)
	}

	otid := ObliviousTransferInitData{
		primitive.Binary{
			Subtype: 0x04,
			Data:    authSerial[:],
		},
		BigIntToString(c),
		false,
	}

	if _, err := DB.GetDataBase("inz", DB.ObliviousTransferInit).InsertOne(context2.Background(), otid); err != nil {
		panic(err)
	}

	return &InitOutput{N: pHex, G: "02", C: hex.EncodeToString(C.Bytes())}
}