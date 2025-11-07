package obliviousTransfer

import "go.mongodb.org/mongo-driver/bson/primitive"

type ObliviousTransferInitData struct {
	AuthSerial primitive.Binary `bson:"authSerial"`
	C          string           `bson:"c"`
	Used       bool             `bson:"used"`
}
