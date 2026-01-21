package commiterStruct

import (
	"bytes"
	"commiter/common"
	"encoding/base64"
	"encoding/json"
	"fmt"
	. "golangShared"
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

	body := CommitAuthPacketBody{
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

func CommitSignKey(key string) error {
	body := CommitSignKeyBody{
		Key: key,
	}

	jsonedBody, err := json.Marshal(body)
	if err != nil {
		panic(err)
	}
	post, err := (&http.Client{}).Post(
		fmt.Sprintf("http://127.0.0.1:%d%s", CommiterPort, CommitSignKeyEndpoint),
		"application/json",
		bytes.NewBuffer(jsonedBody))
	if err != nil {
		panic(err)
	}
	if post.StatusCode != http.StatusOK {
		// panic("response not 200")
	}
	return nil
}

type SingleCommitBody struct {
	CommitmentType common.CommitmentType `json:"commitmentType"`
	Id             uint8                 `json:"id"`
	Data           []byte                `json:"data"`
}

func CommitSingle(commitmentType common.CommitmentType, id uint8, toCommit [64]byte) error {
	body := SingleCommitBody{
		CommitmentType: commitmentType,
		Id:             id,
		Data:           toCommit[:],
	}

	jsonedBody, err := json.Marshal(body)
	if err != nil {
		panic(err)
	}
	post, err := (&http.Client{}).Post(
		fmt.Sprintf("http://127.0.0.1:%d%s", CommiterPort, CommitSingleValueEndpoint),
		"application/json",
		bytes.NewBuffer(jsonedBody))
	if err != nil {
		panic(err)
	}
	if post.StatusCode != http.StatusOK {
		// panic("response not 200")
	}
	return nil
}
