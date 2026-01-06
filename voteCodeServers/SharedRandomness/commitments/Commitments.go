package commitments

import (
	"crypto/sha512"
	"fmt"
	"golangShared/helpers"
	"os"
)

var TrusteeKeyMap map[string]string = map[string]string{
	"1":  "1",
	"2":  "2",
	"3":  "3",
	"4":  "4",
	"5":  "5",
	"6":  "6",
	"7":  "7",
	"8":  "8",
	"9":  "9",
	"10": "10",
}

type Reveal struct {
	Id        string `json:"id"`
	Value     string `json:"value"`
	Signature string `json:"signature"`
}

func NewReveal(val string) *Reveal {
	var reveal Reveal
	id := os.Getenv("ID")

	reveal.Id = id
	reveal.Value = val
	reveal.Signature = id
	return &reveal
}

func (r *Reveal) CheckSign() bool {
	// todo: ogółem to kiedyś to zmienic aby naprawdę sprawdzało podpis, łączenie z pobieraniem zcommitowanego klucza z BB
	return TrusteeKeyMap[r.Id] == r.Signature
}

type Commitment struct {
	Id        string `json:"id"`
	Hash      string `json:"hash"`
	Signature string `json:"signature"`
	value     string
}

func (c *Commitment) CheckSign() bool {
	// todo: ogółem to kiedyś to zmienic aby naprawdę sprawdzało podpis, łączenie z pobieraniem zcommitowanego klucza z BB
	return TrusteeKeyMap[c.Id] == c.Signature
}

func NewCommitment() *Commitment {
	commitment := new(Commitment)
	id := os.Getenv("ID")
	randomValue := helpers.SecureRandomString() // >>> 2^256
	hash := commitment.hash(randomValue[:], id)

	commitment.value = string(randomValue[:])
	commitment.Id = id
	commitment.Signature = id
	commitment.Hash = hash

	// tutaj powinno być podpisywanie, ale na razie je sobie darujemy
	return commitment
}

func (c *Commitment) hash(randomValue []byte, id string) string {
	return fmt.Sprintf("commitment|%x|%s", sha512.Sum512(randomValue[:]), id)
}

func (c *Commitment) GetSecret() string {
	return c.value
}

func (c *Commitment) CheckCommitment(randomValue []byte, id string, value string) bool {
	return value == c.hash(randomValue, id)
}
