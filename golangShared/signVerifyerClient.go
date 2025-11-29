package golangShared

import (
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
)

type VerifyBody struct {
	Sign string `json:"sign"`
}

func VerifySign(sign string) error {
	body := VerifyBody{
		Sign: sign,
	}

	jsonedBody, err := json.Marshal(body)
	if err != nil {
		return err
	}
	post, err := (&http.Client{}).Post(
		fmt.Sprintf("http://127.0.0.1:%d%s", VerifierPort, VerifySignKeyEndpoint),
		"application/json",
		bytes.NewBuffer(jsonedBody))

	if err != nil {
		return err
	}
	if post.StatusCode != http.StatusOK {
		return fmt.Errorf("verify sign failed, status code: %d", post.StatusCode)
	}
	return nil
}
