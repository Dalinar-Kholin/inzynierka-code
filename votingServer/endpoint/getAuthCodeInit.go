package endpoint

import (
	"golangShared/ServerResponse"
	"net/http"
	"votingServer/obliviousTransfer"

	"github.com/gin-gonic/gin"
)

func GetAuthCodeInit(c *gin.Context) {
	var body obliviousTransfer.InitOT
	if err := c.ShouldBindJSON(&body); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
	}

	// jeśli ktoś ma auth code oznacza, że przeszedł walidacje więc nie musimy sprawdzać podpisu
	output, err := obliviousTransfer.InitProtocol(&body)
	if err != nil {
		c.JSON(401, gin.H{"error": err.Error()})
	}

	ServerResponse.ResponseWithSign(c, http.StatusOK, body, output)
}
