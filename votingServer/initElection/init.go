package initElection

import (
	"commiter/common"
	"context"
	"encoding/hex"
	"encoding/json"
	"fmt"
	. "golangShared"
	"golangShared/commiterStruct"
	"helpers"
	"votingServer/DB"
	"votingServer/obliviousTransfer"

	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson/primitive"
)

func CreatePackages() {
	createAuthPackage()
	commiterStruct.FinalCommit(common.AuthPack)
	createVotingPackage()
	commiterStruct.FinalCommit(common.VotePacks)
}

func createVotingPackage() {
	conn := DB.GetDataBase("inz", DB.VoteCollection)
	for range NumberOfPackagesToCreate {
		var newCandidates [NumberOfCandidates]CandidateCode
		copy(newCandidates[:], Candidates)
		helpers.RandomPerm(&newCandidates)
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
		if !commiterStruct.AddToCommit(votingSerial, string(data)) {
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
		newAuth := &AuthPackage{
			AuthSerial: primitive.Binary{
				Subtype: 0x04,    // UUID (standard) – BSON subtype 4
				Data:    guid[:], // 16 bajtów
			},
			AuthCode: [NumberOfAuthCodes]AuthCodePack{
				*generateAuthCodePack(), *generateAuthCodePack(), *generateAuthCodePack(), *generateAuthCodePack(),
			},
			LockPackage: *CreateNewLockPackage(),
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
		if !commiterStruct.AddToCommit(authSerial, string(data)) {
			panic("error while commiting")
		}
	}
}

func generateAuthCodePack() *AuthCodePack {
	c := obliviousTransfer.RandZq()
	size := 128
	b := c.Bytes() // big-endian, bez wiodących zer
	if len(b) < size {
		padded := make([]byte, size)
		copy(padded[size-len(b):], b) // left-pad zerami
		b = padded
	}
	cHex := hex.EncodeToString(b)

	codeOne := helpers.SecureRandomString()
	codeTwo := helpers.SecureRandomString()

	return &AuthCodePack{
		Code: [2]primitive.Binary{
			primitive.Binary{Subtype: 0x00, Data: codeOne[:]},
			primitive.Binary{Subtype: 0x00, Data: codeTwo[:]},
		},
		C:          cHex,
		Status:     UNUSED,
		AccessCode: nil,
		SignStatus: UNUSED,
	}

}

func CreateNewLockPackage() *LockPackage {
	lockCodeTmp := helpers.SecureRandomString()
	lockCode := string(lockCodeTmp[:8])
	randomness := string(lockCodeTmp[8:16])
	commitment := helpers.Commit(lockCode, randomness)

	return &LockPackage{
		LockCode: lockCode, LockCodeCommitment: commitment.String(), LockCodeRandomness: randomness,
	}
}
