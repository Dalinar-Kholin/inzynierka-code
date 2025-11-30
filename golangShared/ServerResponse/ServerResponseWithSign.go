package ServerResponse

import (
	"encoding/json"
	"fmt"
	"golangShared/signer"

	"github.com/gin-gonic/gin"
)

type ServerSignedResponse struct {
	Body any    `json:"body"`
	Sign []byte `json:"sign"`
}

func ResponseWithSign(c *gin.Context, statusCode int, obj any) {
	jsoned, err := json.Marshal(obj)
	if err != nil {
		panic(err)
	}
	signature := signer.Sign(jsoned)
	fmt.Printf("jsoned string := %s\nsign := _%v_\n", string(jsoned), string(signature))

	c.JSON(statusCode, ServerSignedResponse{
		Body: obj,
		Sign: signature,
	})
}
