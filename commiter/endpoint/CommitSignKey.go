package endpoint

import (
	"commiter/common"
	"golangShared/commiterStruct"

	"github.com/gin-gonic/gin"
)

func CommitSignKey(c *gin.Context) {
	var body commiterStruct.CommitSignKeyBody
	if err := c.ShouldBindJSON(&body); err != nil {
		panic(err)
	}

	var key [113]byte
	copy(key[:], body.Key)
	_, err := common.CallCreateSignKey(key)
	if err != nil {
		panic(err)
	}
}
