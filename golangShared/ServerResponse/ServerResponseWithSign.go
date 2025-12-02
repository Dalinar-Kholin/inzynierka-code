package ServerResponse

import (
	"bytes"
	"encoding/json"
	"golangShared/signer"

	"github.com/gin-gonic/gin"
)

type Body struct {
	UserRequest any `json:"userRequest"`
	Content     any `json:"content"`
}

type ServerSignedResponse struct {
	Body Body   `json:"body"`
	Sign []byte `json:"sign"`
}

func ToJSONNoEscape(v any) ([]byte, error) {
	var buf bytes.Buffer

	enc := json.NewEncoder(&buf)
	enc.SetEscapeHTML(false)

	if err := enc.Encode(v); err != nil {
		return nil, err
	}

	out := bytes.TrimRight(buf.Bytes(), "\n")
	return out, nil
}

func ResponseWithSign(c *gin.Context, statusCode int, userRequest any, obj any) {
	content := Body{UserRequest: userRequest, Content: obj}
	jsoned, err := ToJSONNoEscape(content)
	if err != nil {
		panic(err)
	}
	signature := signer.Sign(jsoned)
	c.JSON(statusCode, ServerSignedResponse{
		Body: content,
		Sign: signature,
	})
}
