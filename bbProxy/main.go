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
		sId := c.Request.URL.Query().Get("if")

		t, _ := strconv.Atoi(sType)
		id, _ := strconv.Atoi(sId)

		pda, _, err := solana.FindProgramAddress(
			[][]byte{[]byte("createSingleCommitment"), {uint8(t)}, {uint8(id)}},
			helper.ProgramID,
		)
		if err != nil {
			panic(err)
			return
		}

		acc, err := rp.GetAccountInfo(context.Background(), pda)
		if err != nil {
			panic(err)
			return
		}
		voteCommitmentModel, err := DecodeCommitmentAnchor(acc.Bytes())
		if err != nil {
			panic(err)
			return
		}
		model := &voteCommitmentModel
		based := base64.StdEncoding.EncodeToString(model.Data[:])
		c.JSON(200, gin.H{"data": based})
	})

	r.GET("/voteModel", func(c *gin.Context) {
		authCode := c.Query("authCode")
		model, err := getAnchorVoteModel(authCode)
		if err != nil {
			panic(err)
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
	copy(c.Data[:], payload[1:64+1])

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
