package main

import (
	"SharedRandomness/client"
	"SharedRandomness/commitments"
	"fmt"
	"net/http"
	"time"
)

func main() {
	// zaczytanie klucza aby dla swojego ID móc podpisać commitment
	/*pemBytes, err := os.ReadFile("../ed25519_pub.pem")
	if err != nil {
		panic(err)
	}

	stringed := string(pemBytes)
	res := strings.Replace(stringed, "-----BEGIN PUBLIC KEY-----", "", -1)
	res = strings.Replace(res, "-----END PUBLIC KEY-----", "", -1)
	signKey := strings.TrimSpace(res)
	_ = signKey*/
	// wygenerowanie losowości i wstawienie na BB
	commit := commitments.NewCommitment()

	ea := client.EaRandomnessClient{C: &http.Client{}}
	ea.Commit(*commit)
	for {
		if ea.IsReady() {
			break
		}
		time.Sleep(1 * time.Second)
	}

	rev := commitments.NewReveal(commit.GetSecret())

	ea.RevealSecret(*rev)
	for {
		if randomness := ea.GetRandomness(); randomness != "" {
			fmt.Printf("\nrandomness := %x\n", randomness)
			break
		}
		time.Sleep(1 * time.Second)
	}
}
