package endpoint

import (
	"golangShared/ServerResponse"
	"net/http"
	"votingServer/obliviousTransfer"

	"github.com/gin-gonic/gin"
)

func GetAuthCodeFinal(c *gin.Context) {
	var body obliviousTransfer.UserResponse
	err := c.ShouldBindBodyWithJSON(&body)
	if err != nil {
		c.JSON(401, gin.H{
			"error": err.Error(),
		})
	}
	response := obliviousTransfer.Encrypt(&body)

	// todo: tutaj fajnie by było zcommitować dane na BB

	ServerResponse.ResponseWithSign(c, http.StatusOK, response)
}
