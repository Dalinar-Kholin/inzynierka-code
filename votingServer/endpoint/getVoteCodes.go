package endpoint

import (
	"context"
	"golangShared"
	"votingServer/DB"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
)

type Body struct {
	VoteSerial string `json:"voteSerial"`
}

func GetVoteCodes(c *gin.Context) {
	var body Body
	if err := c.ShouldBindBodyWithJSON(&body); err != nil {
		panic(err)
	}
	idFromBody, err := uuid.Parse(body.VoteSerial)
	if err != nil {
		panic(err)
	}
	bin := primitive.Binary{Subtype: 0x04, Data: idFromBody[:]}
	filter := bson.D{{"voteSerial", bin}}
	var votePack golangShared.VotingPackage
	if err := DB.GetDataBase("inz", DB.VoteCollection).FindOne(context.Background(), filter).Decode(&votePack); err != nil {
		panic(err)
	}
	voteCodes := []string{
		string(votePack.Codes[0][:]),
		string(votePack.Codes[1][:]),
		string(votePack.Codes[2][:]),
		string(votePack.Codes[3][:]),
	}

	c.JSON(200, voteCodes)
}
