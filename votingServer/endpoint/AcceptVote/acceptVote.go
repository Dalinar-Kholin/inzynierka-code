package AcceptVote

import (
	"context"
	"crypto/sha256"
	"encoding/binary"
	"encoding/json"
	"fmt"
	"golangShared"
	"golangShared/signer"
	"votingServer/DB"
	"votingServer/helper"

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
		c.JSON(401, gin.H{"error": err.Error()})
		return
	}
	rp := rpc.New("http://127.0.0.1:8899")

	pda, _, err := solana.FindProgramAddress(
		[][]byte{[]byte("commitVote"), []byte(body.AuthCode[:32]), []byte(body.AuthCode[32:])},
		helper.ProgramID,
	)
	if err != nil {
		c.JSON(401, gin.H{"error": "cant find program"})
		return
	}

	acc, err := rp.GetAccountInfo(context.Background(), pda)
	if err != nil {
		c.JSON(401, gin.H{"error": "cant get account info"})
		return
	}
	voteAnchorModel, err := helper.DecodeVoteAnchor(acc.Bytes())
	if err != nil {
		c.JSON(401, gin.H{"error": "bad data on blockchain"})
		return
	}

	fmt.Printf("acc voteAnchorModel := %v %v\n", string(voteAnchorModel.VoteCode[:]), string(voteAnchorModel.AuthCode[:]))

	bin := primitive.Binary{Subtype: 0x00, Data: []byte(body.AuthCode)}
	filter := bson.D{{"authCode.code", bin}}
	var authPack golangShared.AuthPackage
	if err := DB.GetDataBase("inz", DB.AuthCollection).FindOne(context.Background(), filter).Decode(&authPack); err != nil {
		c.JSON(401, gin.H{"error": "cant find auth package/check spelling"})
		return
	}

	idFromBody, err := uuid.Parse(body.VoteSerial)
	if err != nil {
		panic(err)
	}
	bin = primitive.Binary{Subtype: 0x04, Data: idFromBody[:]}
	filter = bson.D{{"voteSerial", bin}}
	var votePack golangShared.VotingPackage
	if err := DB.GetDataBase("inz", DB.VoteCollection).FindOne(context.Background(), filter).Decode(&votePack); err != nil {
		c.JSON(401, gin.H{
			"error": err.Error(),
		})
		return
	}

	data, _ := json.Marshal(
		DataToSign{
			AuthCode: voteAnchorModel.AuthCode,
			VoteCode: voteAnchorModel.VoteCode,
			Stage:    voteAnchorModel.Stage,
		})
	signature := signer.Sign(data)
	fmt.Printf("signature := %v", signature)

	_, err = helper.SendAcceptVote(
		context.Background(),
		[]byte(body.AuthCode),
		authPack.AuthSerial.Data,
		votePack.VoteSerial.Data,
		authPack.AckCode[:],
		signature)

	if err != nil {
		c.JSON(401, gin.H{"error": err.Error()})
		return
	}
	c.JSON(200, gin.H{
		"code": 200,
	})
}

type DataToSign struct {
	Stage    uint8
	VoteCode [3]byte
	AuthCode [64]byte
}

func disc(method string) []byte {
	sum := sha256.Sum256([]byte("global:" + method))
	return sum[:8]
}

func borshAppendU32LE(dst []byte, v uint32) []byte {
	var buf [4]byte
	binary.LittleEndian.PutUint32(buf[:], v)
	return append(dst, buf[:]...)
}
