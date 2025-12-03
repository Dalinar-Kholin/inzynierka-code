package endpoint

import (
	"encoding/json"
	"golangShared"
	"golangShared/ServerResponse"
	"inz/Storer/StoreClient"
	"io"
	"net/http"
	"votingServer/obliviousTransfer"

	"github.com/gin-gonic/gin"
)

func GetAuthCodeInit(c *gin.Context) {
	var body golangShared.SignedFrontendRequest[obliviousTransfer.InitOT]
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

	// jeśli ktoś ma auth code oznacza, że przeszedł walidacje więc nie musimy sprawdzać podpisu
	output, err := obliviousTransfer.InitProtocol(&body.Body)
	if err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusBadRequest, body, golangShared.ServerError{Error: err.Error()})
	}

	ServerResponse.ResponseWithSign(c, http.StatusOK, body, output)
}
