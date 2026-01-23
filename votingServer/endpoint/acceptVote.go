package endpoint

import (
	"bytes"
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"golangShared"
	"golangShared/ServerResponse"
	"golangShared/signer"
	"io"
	"net/http"
	"votingServer/DB"
	"votingServer/helper"

	"github.com/gagliardetto/solana-go"
	"github.com/gagliardetto/solana-go/rpc"
	"github.com/gin-gonic/gin"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
)

type AcceptBody struct {
	Sign       string `json:"sign"`
	VoteSerial string `json:"voteSerial"`
	AuthCode   string `json:"authCode"`
} // server nie przechowuje <voteSerial, authSerial>

type Response struct {
	Code int `json:"code"`
}

func AcceptVote(c *gin.Context) {
	var body golangShared.SignedFrontendRequest[AcceptBody]
	bodyBytes, err := io.ReadAll(c.Request.Body)
	if err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, bodyBytes, golangShared.ServerError{Error: err.Error()})
		return
	}

	if err := json.Unmarshal(bodyBytes, &body); err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, bodyBytes, golangShared.ServerError{Error: err.Error()})
		return
	}

	bin := primitive.Binary{Subtype: 0x00, Data: []byte(body.Body.AuthCode)}
	filter := bson.D{{"authCode.code", bin}}
	var authPack golangShared.AuthPackage
	if err := DB.GetDataBase("inz", DB.AuthCollection).FindOne(context.Background(), filter).Decode(&authPack); err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusUnauthorized, body, golangShared.ServerError{Error: "cant find auth package/check spelling"})
		return
	}

	filter = bson.D{{"voteSerial", body.Body.VoteSerial}}
	voteAnchorModel, err := getAnchorVoteModel(body.Body)
	if err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusUnauthorized, body, golangShared.ServerError{Error: err.Error()})
		return
	}

	data, _ := json.Marshal(
		DataToSign{
			AuthCode: voteAnchorModel.AuthCode,
			VoteCode: voteAnchorModel.VoteCode,
			Stage:    voteAnchorModel.Stage,
		})

	signature := signer.Sign(data)
	_, err = helper.SendAcceptVote(
		context.Background(),
		[]byte(body.Body.AuthCode),
		authPack.AuthSerial.Data,
		signature)

	if err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, body, golangShared.ServerError{Error: err.Error()})
		return
	}
	var content struct {
		AuthCode string `json:"authCode"`
	}
	content.AuthCode = string(voteAnchorModel.AuthCode[:])
	fmt.Printf("content %v\n", content)

	cnt, err := json.Marshal(content)
	if err != nil {
		panic(err)
	}
	_, err = (&http.Client{}).Post(
		"http://localhost:5000/api/submitvote/authserial",
		"application/json",
		bytes.NewBuffer(cnt))
	if err != nil {
		//panic(err)
	}
	/*if post.StatusCode != 200 {
		panic(post.StatusCode)
	}*/

	ServerResponse.ResponseWithSign(c, 200, body, Response{Code: 200})
}

type DataToSign struct {
	Stage    uint8
	VoteCode [10]byte
	AuthCode [64]byte
}

func getAnchorVoteModel(body AcceptBody) (*helper.Vote, error) {
	rp := rpc.New("http://127.0.0.1:8899")

	pda, _, err := solana.FindProgramAddress(
		[][]byte{[]byte("commitVote"), []byte(body.AuthCode[:32]), []byte(body.AuthCode[32:])},
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
		fmt.Printf("cant decode vote anchor: %v\n", err)
		return nil, errors.New("bad data on blockchain")
	}
	return &voteAnchorModel, nil
}
