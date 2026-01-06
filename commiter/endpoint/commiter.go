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
	"strconv"

	"github.com/gin-gonic/gin"
	"golang.org/x/crypto/sha3"
)

var number = 1

func SingleCommitment(c *gin.Context) {
	var body commiterStruct.SingleCommitBody
	if err := c.ShouldBindJSON(&body); err != nil {
		panic(err)
	}

	var toCommit [64]byte
	copy(toCommit[:], body.Data)
	_, err := common.CallSingleCommitment(body.CommitmentType, body.Id, toCommit)
	if err != nil {
		panic(err)
	}
}

func FinalCommit(c *gin.Context) {
	slices.SortFunc(h.list, func(a, b sha) int {
		return bytes.Compare(a, b)
	})
	for _, x := range h.list {
		fmt.Printf("%v\n", x[:3])
	}

	ct := c.Request.URL.Query().Get("commitment_type")
	if ct == "" {
		panic("bad commitment_type")
	}
	ctInted, err := strconv.Atoi(ct)
	if err != nil {
		panic(err)
	}

	tree, err := merkeleTree.NewMerkleTree(h.list)
	defer func() {
		h = newHashes()
		number++
	}()
	if err != nil {
		panic(err)
	}
	_, err = common.CallCreateCommitmentPack(common.CommitmentType(ctInted), tree.Root())
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
