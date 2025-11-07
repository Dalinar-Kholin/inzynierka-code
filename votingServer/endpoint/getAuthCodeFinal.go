package endpoint

import (
	"net/http"
	"votingServer/obliviousTransfer"

	"github.com/gin-gonic/gin"
)

func GetAuthCodeFinal(c *gin.Context) {
	var body obliviousTransfer.UserResponse
	err := c.ShouldBindBodyWithJSON(&body)
	if err != nil {
		panic(err)
	}
	response := obliviousTransfer.Encrypt(&body)

	// todo: tutaj fajnie by było zcommitować dane na BB

	
	c.JSON(http.StatusOK, response)
}