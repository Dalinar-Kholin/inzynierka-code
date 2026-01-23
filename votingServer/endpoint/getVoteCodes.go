package endpoint

import (
	"context"
	"errors"
	"golangShared"
	"votingServer/DB"

	"github.com/gin-gonic/gin"
	"go.mongodb.org/mongo-driver/bson"
)

func GetVoteCodes(c *gin.Context) {
	permCode := c.Query("perm")
	if permCode == "" {
		panic(errors.New("permCode is empty"))
	}

	var vp golangShared.VotePack
	if err := DB.GetDataBase("inz", DB.VoteCollection).FindOne(context.Background(), bson.M{"permCode": permCode}).Decode(&vp); err != nil {
		panic(err)
	}

	c.JSON(200, vp.EaPack)
}
