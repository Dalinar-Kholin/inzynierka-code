package voting

import (
	"context"
	"crypto/sha256"
	"encoding/json"
	"fmt"
	"math/rand"
	"simpleSolanaSignerServer/commitment"
	"unsafe"

	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson/primitive"
)

func CreatePackages() {
	createAuthPackage()
	createVotingPackage()
}

const numberOfCandidates = 4

var candidates = [][candidatesCodeLength]byte{{'a', 'l', 'a'}, {'b', 'o', 'b'}, {'c', 'a', 't'}, {'d', 'e', 'f'}}

const candidatesCodeLength = 3
const numberOfAuthCodes = 2

const numberOfPackagesToCreate = 100

const AuthCodeLength = 64
const AckCodeLength = 8

type VotingPackage struct {
	Codes      [numberOfCandidates][candidatesCodeLength]byte `bson:"codes" json:"codes"`
	VoteSerial primitive.Binary                               `bson:"voteSerial" json:"voteSerial"`
	Used       bool                                           `bson:"used"`
}

func createVotingPackage() {
	conn := DB.GetDataBase("inz", DB.VoteCollection)
	for range numberOfPackagesToCreate {
		var newCandidates [numberOfCandidates][candidatesCodeLength]byte
		copy(newCandidates[:], candidates)
		randomPerm(&newCandidates)
		serial := uuid.New()
		newPackage := VotingPackage{
			Codes: newCandidates,
			VoteSerial: primitive.Binary{
				Subtype: 0x04,
				Data:    serial[:],
			},
		}
		_, err := conn.InsertOne(context.Background(), newPackage)
		if err != nil {
			panic(err)
		}

		data, err := json.Marshal(newPackage)
		if err != nil {
			panic(err)
		}
		h := sha256.New()
		h.Write(data)
		res := h.Sum(nil)
		fmt.Printf("data : %x\n", res)
		commitment.CommitAuthPack(newPackage.VoteSerial.Data, res)
	}
}

type AuthPackage struct {
	AuthSerial primitive.Binary                        `bson:"authSerial" json:"authSerial"`
	AuthCode   [numberOfAuthCodes][AuthCodeLength]byte `bson:"authCode" json:"authCode"`
	AckCode    [AckCodeLength]byte                     `bson:"ackCode" json:"ackCode"`
	Used       bool                                    `bson:"used"`
}

func createAuthPackage() {
	conn := DB.GetDataBase("inz", DB.AuthCollection)
	for i := range numberOfPackagesToCreate {
		if i%10_000 == 0 {
			fmt.Printf("stworzono := %v\n", i)
		}
		guid := uuid.New()
		ackCode := SecureRandomString()
		newAckCode := *(*[AckCodeLength]byte)(unsafe.Pointer(&ackCode))
		newAuth := AuthPackage{
			AuthSerial: primitive.Binary{
				Subtype: 0x04,    // UUID (standard) – BSON subtype 4
				Data:    guid[:], // 16 bajtów
			},
			AuthCode: [numberOfAuthCodes][AuthCodeLength]byte{
				SecureRandomString(),
				SecureRandomString(),
			},
			AckCode: newAckCode,
		}
		_, err := conn.InsertOne(context.Background(), newAuth)
		if err != nil {
			panic(err)
		}

		data, err := json.Marshal(newAuth)
		if err != nil {
			panic(err)
		}
		h := sha256.New()
		h.Write(data)
		res := h.Sum(nil)
		fmt.Printf("data : %x\n", res)
		commitment.CommitAuthPack(newAuth.AuthSerial.Data, res)
	}
}

func randomPerm(s *[numberOfCandidates][candidatesCodeLength]byte) {
	n := len(s)
	for i := n - 1; i > 0; i-- {
		j := rand.Int() % n
		s[i], s[j] = s[j], s[i]
	}
}

const charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"

func SecureRandomString() [AuthCodeLength]byte {
	var b [AuthCodeLength]byte
	for i := range b {
		n := rand.Int() % len(charset)
		b[i] = charset[n]
	}
	return b
}
