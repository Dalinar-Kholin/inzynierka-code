package client

import (
	"SharedRandomness/commitments"
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
)

type EaRandomnessClient struct {
	C *http.Client
}

const EaPort = 6969

func (e *EaRandomnessClient) Commit(commit commitments.Commitment) {
	jsoned, _ := json.Marshal(commit)

	fmt.Printf("%v\n", string(jsoned))
	res, err := e.C.Post(fmt.Sprintf("http://127.0.0.1:%d/commit",
		EaPort),
		"application/json",
		bytes.NewBuffer(jsoned))
	if err != nil {
		panic(err)
	}
	if res.StatusCode != 200 {
		panic(res.Status)
	}
}

func (e *EaRandomnessClient) IsReady() bool {
	res, err := e.C.Get(fmt.Sprintf("http://127.0.0.1:%d/isReady",
		EaPort))
	if err != nil {
		panic(err)
	}
	if res.StatusCode != 200 {
		return false
	}
	return true
}

type GetCommits struct {
	Data map[string]commitments.Commitment `json:"data"`
}

func (e *EaRandomnessClient) GetCommits() *GetCommits {
	res, err := e.C.Get(fmt.Sprintf("http://127.0.0.1:%d/commit",
		EaPort))
	if err != nil {
		panic(err)
	}
	var getCommits GetCommits
	if err := json.NewDecoder(res.Body).Decode(&getCommits); err != nil {
		panic(err)
	}

	return &getCommits
}

func (e *EaRandomnessClient) RevealSecret(reveal commitments.Reveal) {
	jsoned, _ := json.Marshal(reveal)

	res, err := e.C.Post(fmt.Sprintf("http://127.0.0.1:%d/revealValue",
		EaPort),
		"application/json",
		bytes.NewBuffer(jsoned))
	if err != nil {
		panic(err)
	}
	if res.StatusCode != 200 {
		panic(res.Status)
	}
}

type GetRandomnessBody struct {
	Data string `json:"data"`
}

func (e *EaRandomnessClient) GetRandomness() string {
	res, err := e.C.Get(fmt.Sprintf("http://127.0.0.1:%d/getRevealRandomness", EaPort))
	if err != nil {
		panic(err)
	}
	if res.StatusCode != 200 {
		return ""
	}

	var getRandomness GetRandomnessBody
	if err := json.NewDecoder(res.Body).Decode(&getRandomness); err != nil {
		panic(err)
	}
	fmt.Println(getRandomness.Data)
	return getRandomness.Data
}
