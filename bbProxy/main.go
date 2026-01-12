package main

import (
	"context"
	"encoding/base64"
	"errors"
	"fmt"
	"golangShared"
	"strconv"
	"votingServer/helper"

	"github.com/gagliardetto/solana-go"
	"github.com/gagliardetto/solana-go/rpc"
	"github.com/gin-gonic/gin"
)

func main() {
	r := gin.Default()

	r.GET("/singleCommitments", func(c *gin.Context) {
		rp := rpc.New("http://127.0.0.1:8899")

		sType := c.Request.URL.Query().Get("type")
		sId := c.Request.URL.Query().Get("id")

		t, err := strconv.Atoi(sType)
		if err != nil || t < 0 || t > 255 {
			c.JSON(400, gin.H{"error": "invalid type"})
			return
		}
		id, err := strconv.Atoi(sId)
		if err != nil || id < 0 || id > 255 {
			c.JSON(400, gin.H{"error": "invalid id"})
			return
		}

		pda, _, err := solana.FindProgramAddress(
			[][]byte{[]byte("createSingleCommitment"), {uint8(t)}, {uint8(id)}},
			helper.ProgramID,
		)
		if err != nil {
			c.JSON(500, gin.H{"error": "failed to derive PDA", "details": err.Error()})
			return
		}

		acc, err := rp.GetAccountInfo(context.Background(), pda)
		if err != nil {
			c.JSON(404, gin.H{"error": "account not found", "details": err.Error()})
			return
		}
		data := acc.Bytes()
		if len(data) == 0 {
			c.JSON(404, gin.H{"error": "account has no data"})
			return
		}
		voteCommitmentModel, err := DecodeCommitmentAnchor(data)
		if err != nil {
			c.JSON(500, gin.H{"error": "decode error", "details": err.Error()})
			return
		}
		model := &voteCommitmentModel
		based := base64.StdEncoding.EncodeToString(model.Data[:])
		c.JSON(200, gin.H{"data": based})
	})

	r.GET("/voteModel", func(c *gin.Context) {
		authCode := c.Query("authCode")
		if len(authCode) == 0 {
			c.JSON(400, gin.H{"error": "missing authCode"})
			return
		}
		model, err := getAnchorVoteModel(authCode)
		if err != nil {
			c.JSON(404, gin.H{"error": err.Error()})
			return
		}
		c.JSON(200, model)
	})

	r.Run(fmt.Sprintf(":%d", golangShared.ProxyPort))
}

type Commitment struct {
	CommitmentType uint8    `json:"commitmentType"`
	Id             uint8    `json:"id"`
	Data           [64]byte `json:"data"`
	Bump           uint8
}

func DecodeCommitmentAnchor(data []byte) (Commitment, error) {

	const total = 67
	if len(data) < total {
		return Commitment{}, fmt.Errorf("unexpected length %d, want %d", len(data), total)
	}
	var c Commitment
	payload := data[8:]

	c.CommitmentType = payload[0]
	c.Id = payload[1]
	// Payload layout after discriminator: [0]=ct, [1]=id, [2..65]=to_commit(64B), [66]=bump
	copy(c.Data[:], payload[2:66])

	return c, nil
}
func getAnchorVoteModel(authCode string) (*helper.Vote, error) {
	rp := rpc.New("http://127.0.0.1:8899")

	pda, _, err := solana.FindProgramAddress(
		[][]byte{[]byte("commitVote"), []byte(authCode[:32]), []byte(authCode[32:])},
		helper.ProgramID,
	)
	if err != nil {
		return nil, errors.New("cant find program")
	}

	acc, err := rp.GetAccountInfo(context.Background(), pda)
	if err != nil {
		return nil, errors.New("cant get account info")
	}
	voteAnchorModel, err := helper.DecodeVoteAnchor(acc.Bytes())
	if err != nil {
		return nil, errors.New("bad data on blockchain")
	}
	return &voteAnchorModel, nil
}
