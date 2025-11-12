package endpoint

import (
	"context"
	"crypto/sha256"
	"encoding/binary"
	"fmt"
	"log"

	"github.com/gagliardetto/solana-go"
	"github.com/gagliardetto/solana-go/rpc"
	"github.com/gin-gonic/gin"
	"github.com/mr-tron/base58"
)

type AcceptVoteBody struct {
	Sign       string `json:"sign"`
	AuthSerial string `json:"authSerial"`
	VoteSerial string `json:"voteSerial"`
	AuthCode   string `json:"authCode"`
}

func accountDiscriminator(name string) []byte {
	// Anchor: discriminator = sha256("account:<Name>")[0:8]
	h := sha256.Sum256([]byte("account:" + name))
	return h[:8]
}

func AcceptVote(c *gin.Context) {
	/*var body AcceptVoteBody
	if err := c.ShouldBindJSON(&body); err != nil {
		panic(err)
	}*/
	rp := rpc.New(rpc.MainNetBeta_RPC)

	disc := accountDiscriminator("CastVote")
	discB58 := solana.Base58(base58.Encode(disc))

	accounts, err := rp.GetProgramAccountsWithOpts(ctx, ProgramID, &rpc.GetProgramAccountsOpts{
		Filters: []rpc.RPCFilter{
			{Memcmp: &rpc.RPCFilterMemcmp{
				Offset: 0,
				Bytes:  discB58, // base58-encoded discriminator bytes
			}},
		},
		Encoding:   solana.EncodingBase64, // we'll decode base64 -> bytes below
		Commitment: rpc.CommitmentConfirmed,
	})
	if err != nil {
		log.Fatal(err)
	}

	fmt.Printf("accotunts := \n_%v_\n", accounts)

}

var (
	ProgramID = solana.MustPublicKeyFromBase58("8PuBy6uMn4SRfDDZeJeuYH6hDE9eft1t791mFdUFc5Af")
	Payer     *solana.Wallet
	Client    *rpc.Client
	ctx       = context.Background()
)

func disc(method string) []byte {
	sum := sha256.Sum256([]byte("global:" + method))
	return sum[:8]
}

func borshAppendU32LE(dst []byte, v uint32) []byte {
	var buf [4]byte
	binary.LittleEndian.PutUint32(buf[:], v)
	return append(dst, buf[:]...)
}

// zwracamy
//
