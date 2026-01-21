package main

import (
	"context"
	"crypto/sha256"
	"fmt"
	"golangShared"
	"slices"
	"votingServer/DB"
	"votingServer/helper"

	"github.com/gagliardetto/solana-go"
	"github.com/gagliardetto/solana-go/rpc"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
)

const (
	toOpen                     = 4
	toCount                    = 5
	toOpenButToCountOnSameCard = 6
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
		key := string(vote.VoteSerial[:])
		cards[key] = append(cards[key], vote)
	}

	fmt.Printf("cards len := %d\n", len(cards))

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
				if _, err := helper.SendInterpretVote(context.Background(), v.AuthCode[:], toOpen); err != nil {
					panic(err)
				}
			}
			continue
		}

		for i, v := range cardVotes {
			stage := toOpenButToCountOnSameCard
			if i == index {
				stage = toCount
			}
			if _, err := helper.SendInterpretVote(context.Background(), v.AuthCode[:], uint8(stage)); err != nil {
				panic(err)
			}
		}
	}
}

func getAuthPack(authSerial []byte) *golangShared.AuthPackage {
	bin := primitive.Binary{Subtype: 0x04, Data: authSerial}
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
