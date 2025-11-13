package golangShared

import "go.mongodb.org/mongo-driver/bson/primitive"

type Serial [16]byte
type CandidateCode [candidatesCodeLength]byte

type Status uint8

const (
	UNUSED Status = iota
	USED
	ACTUAL
)

type AckCode [ackCodeLength]byte

const NumberOfCandidates = 4

var Candidates = []CandidateCode{{'a', 'l', 'a'}, {'b', 'o', 'b'}, {'c', 'a', 't'}, {'d', 'e', 'f'}}

const candidatesCodeLength = 3
const NumberOfAuthCodes = 4

const NumberOfPackagesToCreate = 100

const AuthCodeLength = 64
const ackCodeLength = 8

type VotingPackage struct {
	Codes      [NumberOfCandidates]CandidateCode `bson:"codes" json:"codes"`
	VoteSerial primitive.Binary                  `bson:"voteSerial" json:"voteSerial"`
	Used       bool                              `bson:"used"`
}

type AuthPackage struct {
	AuthSerial primitive.Binary                `bson:"authSerial" json:"authSerial"`
	AuthCode   [NumberOfAuthCodes]AuthCodePack `bson:"authCode" json:"authCode"`
	AckCode    AckCode                         `bson:"ackCode" json:"ackCode"`
	Used       bool                            `bson:"used"`
}

type AuthCodePack struct {
	C      string              `bson:"c"`
	Code   [2]primitive.Binary `bson:"code"`
	Status Status              `bson:"status"`
}
