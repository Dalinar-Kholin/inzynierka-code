package golangShared

import (
	"encoding/json"
	"encoding/xml"
	"log"
	"os"

	"github.com/gagliardetto/solana-go"
)

func LoadPrivateKeyFromJSON(path string) *solana.PrivateKey {
	raw, err := os.ReadFile(path)
	if err != nil {
		log.Fatal(err)
	}

	var nums []uint8
	if err := json.Unmarshal(raw, &nums); err != nil {
		log.Fatal(err)
	}

	// 64 bajty: 32 sekret + 32 public
	pk := solana.PrivateKey(nums)
	if len(pk) != 64 {
		log.Fatalf("unexpected key length: %d", len(pk))
	}
	return &pk
}

type SignedFrontendRequest[T any] struct {
	Body T      `json:"body"`
	Sign string `json:"sign"`
}

func IsNullOrEmpty(s *string) bool {
	return s == nil || *s == ""
}

type ServerError struct {
	Error string `json:"error"`
}

type BallotRequest struct {
	XMLName   xml.Name `xml:"Gime"`   // root element
	Ballot    string   `xml:"Ballot"` // <Name>...</Name>
	Key       string   `xml:"Key"`
	Timestamp int64    `xml:"timestamp"`
}

type CommitedBallot struct {
	XMLName    xml.Name `xml:"vote"`
	AuthSerial string   `xml:"AuthSerial"`
	AuthCode   string   `xml:"AuthCode"`
	VoteCode   string   `xml:"VoteCode"`
	ServerSign string   `xml:"ServerSign"`
}

type EaPack struct {
	VoteCodes  []string `json:"voteCodes"`
	AuthSerial string   `json:"authSerial"`
}
