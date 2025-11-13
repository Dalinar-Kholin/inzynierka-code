package AcceptVote

import (
	"context"
	"crypto/sha256"
	"encoding/binary"
	"fmt"
	"golangShared"
	"votingServer/DB"

	"github.com/gagliardetto/solana-go"
	"github.com/gagliardetto/solana-go/rpc"
	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
)

type Body struct {
	Sign       string `json:"sign"`
	VoteSerial string `json:"voteSerial"`
	AuthCode   string `json:"authCode"`
} // server nie przechowuje <voteSerial, authSerial>

func AcceptVote(c *gin.Context) {
	var body Body
	if err := c.ShouldBindJSON(&body); err != nil {
		panic(err)
	}
	rp := rpc.New("http://127.0.0.1:8899")

	pda, _, err := solana.FindProgramAddress(
		[][]byte{[]byte("commitVote"), []byte(body.AuthCode[:32]), []byte(body.AuthCode[32:])},
		ProgramID,
	)
	if err != nil {
		panic(err)
	}

	acc, err := rp.GetAccountInfo(ctx, pda)
	if err != nil {
		panic(err)
	}
	voteAnchorModel, err := decodeVoteAnchor(acc.Bytes())
	if err != nil {
		panic(err)
	}

	fmt.Printf("acc voteAnchorModel := %v %v\n", string(voteAnchorModel.VoteCode[:]), string(voteAnchorModel.AuthCode[:]))

	bin := primitive.Binary{Subtype: 0x00, Data: []byte(body.AuthCode)}
	filter := bson.D{{"authCode.code", bin}}
	var authPack golangShared.AuthPackage
	if err := DB.GetDataBase("inz", DB.AuthCollection).FindOne(context.Background(), filter).Decode(&authPack); err != nil {
		panic(err)
	}

	idFromBody, err := uuid.Parse(body.VoteSerial)
	if err != nil {
		panic(err)
	}
	bin = primitive.Binary{Subtype: 0x04, Data: idFromBody[:]}
	filter = bson.D{{"voteSerial", bin}}
	var votePack golangShared.VotingPackage
	if err := DB.GetDataBase("inz", DB.VoteCollection).FindOne(context.Background(), filter).Decode(&votePack); err != nil {
		panic(err)
	}

	fmt.Printf("auth pack := %v\n", authPack)
	fmt.Printf("vote serial := %v\n", votePack.VoteSerial)

	res, err := SendAcceptVote(
		context.Background(),
		[]byte(body.AuthCode),
		authPack.AuthSerial.Data,
		votePack.VoteSerial.Data,
		authPack.AckCode[:],
		make([]byte, 64))
	if err != nil {
		panic(err)
	}
	fmt.Printf("res data := %v\n", res)
	c.JSON(200, gin.H{
		"code": 200,
	})
}

var (
	ProgramID = solana.MustPublicKeyFromBase58("8PuBy6uMn4SRfDDZeJeuYH6hDE9eft1t791mFdUFc5Af")
	FeePayer  *solana.Wallet
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

type Vote struct {
	Stage      uint8
	VoteSerial [16]byte
	VoteCode   [3]byte
	AuthSerial [16]byte
	AuthCode   [64]byte
	AckCode    [8]byte
	ServerSign [64]byte
	VoterSign  [64]byte
	Bump       uint8
}

func decodeVoteAnchor(data []byte) (Vote, error) {
	const total = 576
	if len(data) != total {
		return Vote{}, fmt.Errorf("unexpected length %d, want %d", len(data), total)
	}
	var v Vote
	// skip discriminator
	payload := data[8:]

	v.Stage = payload[0]

	copy(v.VoteSerial[:], payload[1:1+16])
	copy(v.VoteCode[:], payload[17:17+3])
	copy(v.AuthSerial[:], payload[20:20+16])
	copy(v.AuthCode[:], payload[36:36+64])
	copy(v.AckCode[:], payload[100:100+8])
	copy(v.ServerSign[:], payload[108:108+64])
	copy(v.VoterSign[:], payload[172:172+64])
	v.Bump = payload[236]

	return v, nil
}
