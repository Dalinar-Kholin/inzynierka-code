package endpoint

import (
	"golangShared"
	"golangShared/ServerResponse"
	"inz/Storer/StoreClient"
	"net/http"
	"votingServer/obliviousTransfer"

	"github.com/gin-gonic/gin"
)

func GetAuthCodeFinal(c *gin.Context) {
	var body golangShared.SignedFrontendRequest[obliviousTransfer.UserResponse]
	if err := c.ShouldBindBodyWithJSON(&body); err != nil {
		c.JSON(401, gin.H{
			"error": err.Error(),
		})
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

	response := obliviousTransfer.Encrypt(&body.Body)

	ServerResponse.ResponseWithSign(c, http.StatusOK, body, response)
}
