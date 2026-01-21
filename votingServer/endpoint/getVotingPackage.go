package endpoint

import (
	"context"
	"crypto/sha512"
	"encoding/base64"
	"encoding/json"
	"encoding/xml"
	"errors"
	"fmt"
	"golangShared/ServerResponse"
	"helpers"
	"io"
	"net/http"
	"votingServer/DB"

	. "golangShared"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

type GetVotingPackBody struct {
	SignedXML string `json:"signedXML"`
}

var ServerPubKey string

/*type VotePack struct {
	VoteSerial string   `json:"voteSerial"`
	VoteCodes  []string `json:"voteCodes"`
}*/

type VotingPack struct {
	AuthSerial         string    `json:"authSerial"`
	LockCode           string    `json:"lockCode"`
	LockCodeCommitment string    `json:"lockCodeCommitment"`
	VoteSerials        [2]string `json:"voteSerials"`
	// Votes      [2]VotePack `json:"votes"`
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

	sha := sha512.Sum512([]byte(body.Body.SignedXML))
	fmt.Printf("sha := %x\n", string(sha[:]))
	if err := VerifySign(body.Body.SignedXML); err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, body, ServerError{Error: err.Error()})
		return
	}

	var p BallotRequest
	if err := xml.Unmarshal([]byte(body.Body.SignedXML), &p); err != nil {
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
		return
	}

	/*jsoned, _ := ServerResponse.ToJSONNoEscape(body)
	err = StoreClient.Client(StoreClient.RequestBody{
		AuthCode: &result.AuthCode,
		AuthCode:   nil,
		Data:       string(jsoned),
	})
	if err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusInternalServerError, body, ServerError{Error: err.Error()}) // to raczej nie powinno się wydarzyć chyba że server przestanie działać
		return
	}*/
	pack.VoteSerials = communicateWithFakeSgx([]byte(body.Body.SignedXML), pack.AuthSerial)
	ServerResponse.ResponseWithSign(c, http.StatusOK, body, pack)
	// c.JSON(200, result)
}

func communicateWithFakeSgx(xml []byte, authSerial string) [2]string {
	sha := sha512.Sum512(xml)

	based := base64.URLEncoding.EncodeToString(sha[:])

	u, err := uuid.Parse(authSerial)
	if err != nil {
		panic(err)
	}

	permCodeBytes := helpers.SecureRandomString()
	PermCodeString := base64.URLEncoding.EncodeToString(permCodeBytes[:])

	filter := bson.M{
		"authSerial": primitive.Binary{
			Subtype: 0x04, // standard UUID
			Data:    u[:], // 16 bytes
		},
	}

	// update: set status = USED for all array elements that match the array filter
	update := bson.M{
		"$set": bson.M{
			"permCode": PermCodeString,
		},
	}

	_, err = DB.GetDataBase("inz", DB.AuthCollection).UpdateOne(
		context.Background(),
		filter,
		update,
	)
	if err != nil {
		panic(err)
	}

	response, err := http.Get(fmt.Sprintf("http://127.0.0.1:%d/ea?sha=%s&perm=%s", SGXPort, based, PermCodeString))
	if err != nil {
		panic(err)
	}

	defer response.Body.Close()
	b, err := io.ReadAll(response.Body)
	if err != nil {
		panic(err)
	}
	var arr []EaPack
	err = json.Unmarshal(b, &arr)
	if err != nil {
		panic(err)
	}
	fmt.Printf("arr = %+v\n", arr)
	return [2]string{
		arr[0].AuthSerial,
		arr[1].AuthSerial,
	}
}

func GetVotingPackage() (*VotingPack, error) {
	coll := DB.GetDataBase("inz", "authCard")
	authPackage, err := popRandomDocumentTx[AuthPackage](context.Background(), coll)

	if err != nil {
		return nil, err
	}

	authSerial, _ := uuid.FromBytes(authPackage.AuthSerial.Data)

	return &VotingPack{
		AuthSerial:         authSerial.String(),
		LockCode:           authPackage.LockPackage.LockCode,
		LockCodeCommitment: authPackage.LockPackage.LockCodeCommitment,
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
