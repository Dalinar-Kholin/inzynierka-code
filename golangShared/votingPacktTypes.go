package golangShared

import "go.mongodb.org/mongo-driver/bson/primitive"

type Serial [16]byte
type CandidateCode [candidatesCodeLength]byte
type AuthCode [authCodeLength]byte
type AckCode [ackCodeLength]byte

const NumberOfCandidates = 4

var Candidates = []CandidateCode{{'a', 'l', 'a'}, {'b', 'o', 'b'}, {'c', 'a', 't'}, {'d', 'e', 'f'}}

const candidatesCodeLength = 3
const NumberOfAuthCodes = 2

const NumberOfPackagesToCreate = 100

const authCodeLength = 64
const ackCodeLength = 8

type VotingPackage struct {
	Codes      [NumberOfCandidates]CandidateCode `bson:"codes" json:"codes"`
	VoteSerial primitive.Binary                  `bson:"voteSerial" json:"voteSerial"`
	Used       bool                              `bson:"used"`
}

type AuthPackage struct {
	AuthSerial primitive.Binary            `bson:"authSerial" json:"authSerial"`
	AuthCode   [NumberOfAuthCodes]AuthCode `bson:"authCode" json:"authCode"`
	AckCode    AckCode                     `bson:"ackCode" json:"ackCode"`
	Used       bool                        `bson:"used"`
}
