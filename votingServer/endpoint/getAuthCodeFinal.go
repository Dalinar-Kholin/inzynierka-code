package endpoint

import (
	"context"
	"encoding/json"
	"errors"
	"golangShared"
	"golangShared/ServerResponse"
	"inz/Storer/StoreClient"
	"io"
	"net/http"
	"votingServer/DB"
	"votingServer/obliviousTransfer"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
	"go.mongodb.org/mongo-driver/mongo"
)

type EncryptResponse struct {
	OtData   *obliviousTransfer.EncryptResponse `json:"otData"`
	PermCode string                             `json:"permCode"`
	R        string                             `json:"r"`
}

func GetAuthCodeFinal(c *gin.Context) {
	var body golangShared.SignedFrontendRequest[obliviousTransfer.UserResponse]
	bodyBytes, err := io.ReadAll(c.Request.Body)
	if err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, bodyBytes, golangShared.ServerError{Error: err.Error()})
		return
	}

	if err := json.Unmarshal(bodyBytes, &body); err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, bodyBytes, golangShared.ServerError{Error: err.Error()})
		return
	}
	jsoned, _ := ServerResponse.ToJSONNoEscape(body) // parsuejy 2 razy do jsona na razie ale nie mam siły tego teraz zmieniać
	err = StoreClient.Client(StoreClient.RequestBody{
		AuthSerial: &body.Body.AuthSerial,
		AuthCode:   nil,
		Data:       string(jsoned),
	})
	if err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusInternalServerError, body, golangShared.ServerError{Error: err.Error()}) // to raczej nie powinno się wydarzyć chyba że server przestanie działać
		return
	}

	response, err := obliviousTransfer.Encrypt(&body.Body)
	if err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, body, golangShared.ServerError{Error: err.Error()})
		return
	}

	u, err := uuid.Parse(body.Body.AuthSerial)
	var Auth golangShared.AuthPackage
	if err := DB.GetDataBase("inz", DB.AuthCollection).FindOne(
		context.Background(),
		bson.M{
			"authSerial": primitive.Binary{
				Subtype: 0x04,
				Data:    u[:],
			},
		},
	).Decode(&Auth); errors.Is(err, mongo.ErrNoDocuments) {
		ServerResponse.ResponseWithSign(c, http.StatusOK, body, err.Error())
		return
	}

	resp := EncryptResponse{
		OtData:   response,
		PermCode: Auth.PermCode,
		R:        Auth.LockPackage.LockCodeRandomness,
	}

	ServerResponse.ResponseWithSign(c, http.StatusOK, body, resp)
}
