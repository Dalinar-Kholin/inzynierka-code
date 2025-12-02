package endpoint

import (
	"golangShared"
	"golangShared/ServerResponse"
	"inz/Storer/StoreClient"
	"net/http"
	"votingServer/obliviousTransfer"

	"github.com/gin-gonic/gin"
)

func GetAuthCodeInit(c *gin.Context) {
	var body golangShared.SignedFrontendRequest[obliviousTransfer.InitOT]
	if err := c.ShouldBindJSON(&body); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
	}

	jsoned, _ := ServerResponse.ToJSONNoEscape(body) // parsuejy 2 razy do jsona na razie ale nie mam siły tego teraz zmieniać
	err := StoreClient.Client(StoreClient.RequestBody{
		AuthSerial: &body.Body.AuthSerial,
		AuthCode:   nil,
		Data:       string(jsoned),
	})
	if err != nil {
		panic(err) // to raczej nie powinno się wydarzyć chyba że server przestanie działać
	}

	// jeśli ktoś ma auth code oznacza, że przeszedł walidacje więc nie musimy sprawdzać podpisu
	output, err := obliviousTransfer.InitProtocol(&body.Body)
	if err != nil {
		c.JSON(401, gin.H{"error": err.Error()})
	}

	ServerResponse.ResponseWithSign(c, http.StatusOK, body, output)
}
