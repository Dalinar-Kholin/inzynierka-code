package StoreClient

import (
	"bytes"
	"encoding/json"
	"fmt"
	"golangShared"
	"net/http"
)

type RequestBody struct {
	AuthSerial *string `json:"authSerial,omitempty"`
	AuthCode   *string `json:"authCode,omitempty"`
	Data       string  `json:"data"`
}

func Client(body RequestBody) error {
	jsoned, err := json.Marshal(body)
	if err != nil {
		return err
	}

	post, err := (&http.Client{}).Post(
		fmt.Sprintf("http://127.0.0.1:%d%s", golangShared.StorerPort, golangShared.StorerEndpoint),
		"application/json",
		bytes.NewBuffer(jsoned))
	if err != nil {
		return err
	}
	if post.StatusCode != 200 {
		return fmt.Errorf("post status code: %d", post.StatusCode)
	}

	return nil
}
