package endpoint

import (
	"context"
	"votingServer/helper"

	"github.com/gin-gonic/gin"
)

type body struct {
	AuthCode   string `json:"authCode"`
	VoteVector string `json:"voteVector"`
}

func UpdateVote(c *gin.Context) {
	var body body
	if err := c.ShouldBindJSON(&body); err != nil {
		panic(err)
	}
	_, err := helper.UpdateVoteVector(context.Background(), []byte(body.AuthCode), []byte(body.VoteVector))
	if err != nil {
		panic(err)
	}
}
