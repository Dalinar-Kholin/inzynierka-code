package endpoint

import (
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
	output := obliviousTransfer.InitProtocol(&body)

	// todo: tutaj fajnie by było zcommitować dane na BB

	c.JSON(http.StatusOK, output)
}
