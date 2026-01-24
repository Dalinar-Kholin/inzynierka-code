package main

import (
	"bytes"
	"compress/gzip"
	"context"
	"crypto/sha256"
	"encoding/json"
	"encoding/xml"
	"fmt"
	"golangShared"
	"golangShared/signer"
	"io"
	"net/http"
	"slices"
	"votingServer/DB"
	"votingServer/helper"

	"github.com/gagliardetto/solana-go"
	"github.com/gagliardetto/solana-go/rpc"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
)

func main() {
	rp := rpc.New("http://127.0.0.1:8899")

	payer := WalletFromPrivateKey(golangShared.LoadPrivateKeyFromJSON("../../signer.json"))
	helper.FeePayer = payer

	votes, err := GetAllVotes(rp, helper.ProgramID)
	if err != nil {
		panic(err)
	}

	cards := make(map[string][]*helper.Vote)

	for _, vote := range votes {
		_, err := gunzip(vote.VoterSign)
		if err != nil {
			panic(err)
		}
		/*		if err := golangShared.VerifySign(string(res)); err != nil {
				panic(err)
			}*/
		key := string(vote.VoteSerial[:])
		cards[key] = append(cards[key], vote)
	}

	for _, cardVotes := range cards {
		if len(cardVotes) == 0 {
			continue
		}

		authPack := getAuthPack(cardVotes[0].AuthSerial[:])

		index := slices.IndexFunc(cardVotes, func(n *helper.Vote) bool {
			return string(n.LockCode[:]) == authPack.LockPackage.LockCode
		})

		if index == -1 {
			for _, v := range cardVotes {
				if _, err := helper.SendInterpretVote(context.Background(), v.AuthCode[:], uint8(golangShared.ToOpen)); err != nil {
					panic(err)
				}
			}
			continue
		}

		for i, v := range cardVotes {
			// res, _ := gunzip(v.VoterSign)
			/*data := parseXML(res)
			if checkCorrectness(data, authPack) {
				panic("correctness fail XD")
			}*/
			stage := golangShared.ToOpenButToCountOnSameCard
			if i == index {
				stage = golangShared.ToCount
				// curl -X POST http://localhost:5000/api/forCounting -d 'authCodeValue'
				_, err = (&http.Client{}).Post(
					"http://localhost:5000/api/forCounting",
					"application/json",
					bytes.NewBuffer(v.AuthCode[:]))
				if err != nil {
					fmt.Printf("coś nie bangle := \n\n%v\n\n", err)
					//panic(err)
				}
			}
			if _, err := helper.SendInterpretVote(context.Background(), v.AuthCode[:], uint8(stage)); err != nil {
				panic(err)
			}
		}
	}
}

func parseXML(data []byte) *golangShared.CommitedBallot {
	var cb golangShared.CommitedBallot
	if err := xml.Unmarshal(data, &cb); err != nil {
		panic(err)
		return nil
	}
	return &cb
}

func checkCorrectness(cb *golangShared.CommitedBallot, auth *golangShared.AuthPackage) bool {
	var ac [64]byte
	copy(ac[:], cb.AuthCode)
	var vc [10]byte
	copy(vc[:], cb.VoteCode)
	data, _ := json.Marshal(
		golangShared.DataToSign{
			AuthCode: ac,
			VoteCode: vc,
			Stage:    uint8(golangShared.USED),
		})

	return cb.AuthSerial == string(auth.AuthSerial.Data) && slices.IndexFunc(auth.AuthCode[:], func(e golangShared.AuthCodePack) bool {
		return string(e.Code[0].Data) == cb.AuthCode || string(e.Code[1].Data) == cb.AuthCode
	}) != -1 && signer.Verify(data, []byte(cb.ServerSign))
}

func getAuthPack(authSerial []byte) *golangShared.AuthPackage {
	bin := primitive.Binary{Subtype: 0x00, Data: authSerial}
	filter := bson.D{{"authSerial", bin}}
	var authPack golangShared.AuthPackage
	if err := DB.GetDataBase("inz", DB.AuthCollection).FindOne(context.Background(), filter).Decode(&authPack); err != nil {
		panic(err)
	}
	return &authPack
}

func GetAllVotes(client *rpc.Client, programID solana.PublicKey) ([]*helper.Vote, error) {
	ctx := context.Background()

	disc := accountDiscriminator("Vote")

	opts := &rpc.GetProgramAccountsOpts{
		Filters: []rpc.RPCFilter{
			{
				Memcmp: &rpc.RPCFilterMemcmp{
					Offset: 0,    // discriminator jest na początku
					Bytes:  disc, // 8 bajtów discriminatora
				},
			},
		},
	}

	res, err := client.GetProgramAccountsWithOpts(ctx, programID, opts)
	if err != nil {
		return nil, err
	}

	votes := make([]*helper.Vote, 0, len(res))

	for _, acc := range res {
		data := acc.Account.Data.GetBinary()
		voteAnchorModel, err := helper.DecodeVoteAnchor(data)
		if err != nil {
			panic(err)
		}

		votes = append(votes, &voteAnchorModel)
	}

	return votes, nil
}

func accountDiscriminator(name string) []byte {
	h := sha256.Sum256([]byte("account:" + name))
	return h[:8]
}

func WalletFromPrivateKey(pk *solana.PrivateKey) *solana.Wallet {
	return &solana.Wallet{PrivateKey: *pk}
}

func gunzip(data []byte) ([]byte, error) {
	r, err := gzip.NewReader(bytes.NewReader(data))
	if err != nil {
		return nil, err
	}
	defer r.Close()

	return io.ReadAll(r)
}
