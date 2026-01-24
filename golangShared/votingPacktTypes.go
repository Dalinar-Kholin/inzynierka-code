package golangShared

import (
	"go.mongodb.org/mongo-driver/bson/primitive"
)

type Serial [16]byte

type Status uint8

const (
	UNUSED Status = iota
	USED
	ACTUAL
	COMMITED
	ToOpen
	ToCount
	ToOpenButToCountOnSameCard
)

const NumberOfAuthCodes = 4

const NumberOfPackagesToCreate = 100

const AuthCodeLength = 64

type AuthPackage struct {
	AuthSerial  primitive.Binary                `bson:"authSerial" json:"authSerial"`
	AuthCode    [NumberOfAuthCodes]AuthCodePack `bson:"authCode" json:"authCode"`
	Used        bool                            `bson:"used" json:"-"`
	LockPackage LockPackage                     `bson:"lockPackage"`
	PermCode    string                          `bson:"permCode" json:"-"`
}

type LockPackage struct {
	LockCode           string `bson:"lockCode"`
	LockCodeCommitment string `bson:"lockCodeCommitment"`
	LockCodeRandomness string `bson:"lockCodeRandomness"`
}

type AuthCodePack struct {
	C          string              `bson:"c"`
	Code       [2]primitive.Binary `bson:"code"`
	Status     Status              `bson:"status"`
	AccessCode *primitive.Binary   `bson:"accessCode"` // for transaction signing reason
	SignStatus Status              `bson:"SignStatus"` //
}

type VotePack struct {
	EaPack   []EaPack `bson:"eaPack"`
	PermCode string   `bson:"permCode"`
}

type DataToSign struct {
	Stage    uint8
	VoteCode [10]byte
	AuthCode [64]byte
}
