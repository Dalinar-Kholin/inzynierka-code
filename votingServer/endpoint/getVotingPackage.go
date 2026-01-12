package endpoint

import (
	"context"
	"encoding/json"
	"encoding/xml"
	"errors"
	"fmt"
	"golangShared/ServerResponse"
	"io"
	"net/http"
	"votingServer/DB"

	. "golangShared"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

type GetVotingPackBody struct {
	BasedSign string `json:"basedSign"`
}

type ballotRequest struct {
	XMLName xml.Name `xml:"Gime"`   // root element
	Ballot  string   `xml:"Ballot"` // <Name>...</Name>
	Key     string   `xml:"Key"`
}

var ServerPubKey string

type VotePack struct {
	VoteSerial string   `json:"voteSerial"`
	VoteCodes  []string `json:"voteCodes"`
}

type VotingPack struct {
	AuthSerial string      `json:"authSerial"`
	Votes      [2]VotePack `json:"votes"`
}

func GetVotingPack(c *gin.Context) {
	var body SignedFrontendRequest[GetVotingPackBody]
	bodyBytes, err := io.ReadAll(c.Request.Body)
	if err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, bodyBytes, ServerError{Error: err.Error()})
		return
	}

	if err := json.Unmarshal(bodyBytes, &body); err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, bodyBytes, ServerError{Error: err.Error()})
		return
	}

	if err := VerifySign(body.Body.BasedSign); err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, body, ServerError{Error: err.Error()})
		return
	}

	var p ballotRequest
	if err := xml.Unmarshal([]byte(body.Body.BasedSign), &p); err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, body, ServerError{Error: err.Error()})
		return
	}
	if p.Ballot != ServerPubKey {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, body, ServerError{Error: "podpis nie odnosi się do obecnego głosowania"})
		return
	}

	pack, err := GetVotingPackage()
	if err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, body, ServerError{Error: err.Error()})
	}

	/*jsoned, _ := ServerResponse.ToJSONNoEscape(body)
	err = StoreClient.Client(StoreClient.RequestBody{
		AuthSerial: &result.AuthSerial,
		AuthCode:   nil,
		Data:       string(jsoned),
	})
	if err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusInternalServerError, body, ServerError{Error: err.Error()}) // to raczej nie powinno się wydarzyć chyba że server przestanie działać
		return
	}*/

	ServerResponse.ResponseWithSign(c, http.StatusOK, body, pack)
	// c.JSON(200, result)
}

func GetVotingPackage() (*VotingPack, error) {
	coll := DB.GetDataBase("inz", "authCard")
	authPackage, err := popRandomDocumentTx[AuthPackage](context.Background(), coll)

	if err != nil {
		return nil, err
	}

	authSerial, _ := uuid.FromBytes(authPackage.AuthSerial.Data)

	coll = DB.GetDataBase("inz", "votesCard")
	var vp1 VotePack
	votingPackage, err := popRandomDocumentTx[VotingPackage](context.Background(), coll)
	if err != nil {
		return nil, err
	}
	voteSerial, _ := uuid.FromBytes(votingPackage.VoteSerial.Data)
	vp1.VoteSerial = voteSerial.String()
	vp1.VoteCodes = []string{
		string(votingPackage.Codes[0][:]),
		string(votingPackage.Codes[1][:]),
		string(votingPackage.Codes[2][:]),
		string(votingPackage.Codes[3][:]),
	}

	var vp2 VotePack
	votingPackage, err = popRandomDocumentTx[VotingPackage](context.Background(), coll)
	if err != nil {
		return nil, err
	}
	voteSerial, _ = uuid.FromBytes(votingPackage.VoteSerial.Data)
	vp2.VoteSerial = voteSerial.String()
	vp2.VoteCodes = []string{
		string(votingPackage.Codes[0][:]),
		string(votingPackage.Codes[1][:]),
		string(votingPackage.Codes[2][:]),
		string(votingPackage.Codes[3][:]),
	}

	return &VotingPack{
		AuthSerial: authSerial.String(),
		Votes:      [2]VotePack{vp1, vp2},
	}, nil
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
