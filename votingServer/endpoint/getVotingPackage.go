package endpoint

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"votingServer/DB"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
	. "golangShared"
)

type GetVotingPackBody struct {
	BasedSign string `json:"basedSign"`
}

type VotingPack struct {
	AuthSerial string   `json:"authSerial"`
	VoteSerial string   `json:"voteSerial"`
	VoteCodes  []string `json:"voteCodes"`
	AckCode    string   `json:"ackCode"`
	// AuthCode   nil      `json:"authCode"` // to będzie przekazywane oblivious transferem
}

func GetVotingPack(c *gin.Context) {

	var bodyData GetVotingPackBody
	if err := json.NewDecoder(c.Request.Body).Decode(&bodyData); err != nil {
		panic(err)
	}

	// todo: sprawdzneie sygnatury czy jest poprawna i czy koperta powinna zostać wydana

	coll := DB.GetDataBase("inz", "votesCard")
	votingPackage, err := popRandomDocumentTx[VotingPackage](context.Background(), coll)

	if err != nil {
		panic(err)
	}

	coll = DB.GetDataBase("inz", "authCard")
	authPackage, err := popRandomDocumentTx[AuthPackage](context.Background(), coll)

	if err != nil {
		panic(err)
	}

	authSerial, _ := uuid.FromBytes(authPackage.AuthSerial.Data)
	voteSerial, _ := uuid.FromBytes(votingPackage.VoteSerial.Data)
	voteCodes := []string{
		string(votingPackage.Codes[0][:]),
		string(votingPackage.Codes[1][:]),
		string(votingPackage.Codes[2][:]),
		string(votingPackage.Codes[3][:]),
	}

	result := VotingPack{
		AuthSerial: authSerial.String(),
		VoteSerial: voteSerial.String(),
		VoteCodes:  voteCodes,
		AckCode:    string(authPackage.AckCode[:]),
	}

	c.JSON(200, result)
}

func popRandomDocumentTx[T any](ctx context.Context, coll *mongo.Collection) (*T, error) {
	for attempt := 0; attempt < 100; attempt++ {
		cur, err := coll.Aggregate(ctx, mongo.Pipeline{
			{{"$match", bson.D{{Key: "used", Value: false}}}},
			{{"$sample", bson.D{{Key: "size", Value: 1}}}},
			{{"$project", bson.D{{Key: "_id", Value: 1}}}},
		})
		if err != nil {
			return nil, err
		}

		var idHolder struct {
			ID any `bson:"_id"`
		}
		hasDoc := cur.Next(ctx)
		_ = cur.Close(ctx)

		if !hasDoc {
			if err := cur.Err(); err != nil {
				return nil, err
			}
			return nil, mongo.ErrNoDocuments
		}

		if err := cur.Decode(&idHolder); err != nil {
			return nil, err
		}

		var out T
		err = coll.FindOneAndUpdate(
			ctx,
			bson.M{"_id": idHolder.ID, "used": false},
			bson.M{"$set": bson.M{"used": true}},
			options.FindOneAndUpdate().SetReturnDocument(options.After),
		).Decode(&out)

		if err == nil {
			return &out, nil
		}
		if !errors.Is(err, mongo.ErrNoDocuments) {
			return nil, err
		}
	}

	return nil, fmt.Errorf("could not reserve a document after %d attempts", 100)

}
