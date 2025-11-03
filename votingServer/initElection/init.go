package initElection

import (
	"context"
	"crypto/rand"
	"encoding/json"
	"fmt"
	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson/primitive"
	. "golangShared"
	"math/big"
	"unsafe"
	"votingServer/DB"
	"votingServer/commitment"
)

func CreatePackages() {
	createAuthPackage()
	commitment.FinalCommit()
	createVotingPackage()
	commitment.FinalCommit()
}

func createVotingPackage() {
	conn := DB.GetDataBase("inz", DB.VoteCollection)
	for range NumberOfPackagesToCreate {
		var newCandidates [NumberOfCandidates]CandidateCode
		copy(newCandidates[:], Candidates)
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
		var votingSerial Serial
		copy(votingSerial[:], newPackage.VoteSerial.Data)
		if !commitment.AddToCommit(votingSerial, string(data)) {
			panic("error when commiting")
		}

	}
}

func createAuthPackage() {
	conn := DB.GetDataBase("inz", DB.AuthCollection)
	for i := range NumberOfPackagesToCreate {
		if i%10_000 == 0 {
			fmt.Printf("stworzono := %v\n", i)
		}
		guid := uuid.New()
		ackCode := SecureRandomString()
		newAckCode := *(*AckCode)(unsafe.Pointer(&ackCode))
		newAuth := AuthPackage{
			AuthSerial: primitive.Binary{
				Subtype: 0x04,    // UUID (standard) – BSON subtype 4
				Data:    guid[:], // 16 bajtów
			},
			AuthCode: [NumberOfAuthCodes]AuthCode{
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

		var authSerial Serial
		copy(authSerial[:], newAuth.AuthSerial.Data)
		if !commitment.AddToCommit(authSerial, string(data)) {
			panic("error while commiting")
		}
	}
}

func cryptoRandInt(n int) int {
	if n <= 0 {
		return 0
	}
	max := big.NewInt(int64(n))
	v, err := rand.Int(rand.Reader, max)
	if err != nil {
		panic(err)
	}
	return int(v.Int64())
}

func randomPerm(s *[NumberOfCandidates]CandidateCode) {
	n := len(s)
	for i := n - 1; i > 0; i-- {
		j := cryptoRandInt(i + 1)
		s[i], s[j] = s[j], s[i]
	}
}

const charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"

func SecureRandomString() AuthCode {
	var out AuthCode
	for i := range out {
		out[i] = charset[cryptoRandInt(len(charset))]
	}
	return out
}
