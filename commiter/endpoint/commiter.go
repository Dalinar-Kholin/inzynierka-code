package endpoint

import (
	"encoding/base64"
	"encoding/json"
	"fmt"
	"github.com/gin-gonic/gin"
	"golang.org/x/crypto/sha3"
	. "golangShared"
	"golangShared/commiterStruct"
)

func FinalCommit(c *gin.Context) {
	hash := fmt.Sprintf("%s", h.list)
	fmt.Printf("%s\n", hash)
	h = newHashes()
	c.Status(200)
}

func AddAuthPackage(c *gin.Context) {
	var body commiterStruct.CommitAuthPacketBody
	if err := json.NewDecoder(c.Request.Body).Decode(&body); err != nil {
		panic(err)
	}

	var as Serial
	var tmp []byte
	tmp, err := base64.StdEncoding.DecodeString(body.AuthSerial)
	copy(as[:], tmp)

	if err != nil {
		panic(err)
	}
	sha := sha3.New256()
	sha.Write([]byte(body.Data))
	shaRes := sha.Sum(nil)

	_, err = base64.StdEncoding.DecodeString(body.AuthSerial)
	h.Add(shaRes)
	c.Status(200)
}

type sha []byte

type Hashes struct {
	list   []sha
	number int
}

func (h *Hashes) Add(newSha sha) {
	h.list[h.number] = newSha
	h.number++
}

func newHashes() Hashes {
	return Hashes{
		list:   make([]sha, NumberOfPackagesToCreate),
		number: 0,
	}
}

var h = newHashes()
