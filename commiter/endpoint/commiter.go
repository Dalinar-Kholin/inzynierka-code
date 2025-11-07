package endpoint

import (
	"bytes"
	"commiter/common"
	"commiter/merkeleTree"
	"encoding/base64"
	"encoding/json"
	"fmt"
	. "golangShared"
	"golangShared/commiterStruct"
	"slices"

	"github.com/gin-gonic/gin"
	"golang.org/x/crypto/sha3"
)

var number = 1

func FinalCommit(c *gin.Context) {
	slices.SortFunc(h.list, func(a, b sha) int {
		return bytes.Compare(a, b)
	})
	for _, x := range h.list {
		fmt.Printf("%v\n", x[:3])
	}

	tree, err := merkeleTree.NewMerkleTree(h.list)
	defer func() {
		h = newHashes()
		number++
	}()
	if err != nil {
		panic(err)
	}
	var authSerial [16]byte
	authSerial[0] = byte(number % 255)
	_, err = common.CallCreateCommitmentPack(authSerial, tree.Root())
	if err != nil {
		panic(err)
	}

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

type sha = []byte

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
