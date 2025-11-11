package commitment

import (
	"bytes"
	"commiter/common"
	"encoding/base64"
	"encoding/json"
	"fmt"
	. "golangShared"
	"golangShared/commiterStruct"
	"net/http"
)

func FinalCommit(commitmentType common.CommitmentType) bool {
	res, err := (&http.Client{}).Get(
		fmt.Sprintf("http://127.0.0.1:%d%s?commitment_type=%d", CommiterPort, FinalCommitEndpoint, commitmentType),
	)
	if err != nil {
		panic(err)
		return false
	}
	return res.StatusCode == http.StatusOK
}

func AddToCommit(auth Serial, data string) bool {
	basedAuth := base64.StdEncoding.EncodeToString(auth[:])

	body := commiterStruct.CommitAuthPacketBody{
		AuthSerial: basedAuth,
		Data:       data,
	}
	jsonedBody, err := json.Marshal(body)
	if err != nil {
		panic(err)
	}
	post, err := (&http.Client{}).Post(
		fmt.Sprintf("http://127.0.0.1:%d%s", CommiterPort, AddCommitPackEndpoint),
		"application/json",
		bytes.NewBuffer(jsonedBody))
	if err != nil {
		panic(err)
	}
	if post.StatusCode != http.StatusOK {
		panic("response not 200")
	}
	return true
}

func Commit() {

}
