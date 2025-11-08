package obliviousTransfer

import (
	context2 "context"
	"encoding/hex"
	"errors"
	"go.mongodb.org/mongo-driver/mongo/options"
	"golang.org/x/net/context"
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
	var Auth golangShared.AuthPackage
	authSerial, err := uuid.Parse(initOt.AuthSerial)
	if err != nil {
		panic(err)
	}

	filter := bson.M{
		"authSerial": primitive.Binary{
			Subtype: 0x04,          // standard UUID
			Data:    authSerial[:], // 16 bytes
		},
		"authCode": bson.M{
			"$elemMatch": bson.M{"status": golangShared.ACTUAL},
		},
	}

	// update: set status = USED for all array elements that match the array filter
	update := bson.M{
		"$set": bson.M{
			"authCode.$[elem].status": golangShared.USED,
		},
	}

	// array filter: only elements whose status is ACTUAL
	opts := options.Update().SetArrayFilters(options.ArrayFilters{
		Filters: []interface{}{
			bson.M{"elem.status": golangShared.ACTUAL},
		},
	})

	_, err = DB.GetDataBase("inz", DB.AuthCollection).UpdateMany(
		context.Background(),
		filter,
		update,
		opts,
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

	authPack, err := FindUnused(&Auth)
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

	c := HexToBigInt(authPack.C)
	C := modExp(g, c, p)

	return &InitOutput{N: PHex, G: "02", C: hex.EncodeToString(C.Bytes())}
}
