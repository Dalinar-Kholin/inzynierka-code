package endpoints

import (
	"crypto/rand"
	"crypto/sha512"
	"encoding/base64"
	"fmt"
	"golangShared"
	"helpers"
	"io"
	"math/big"
	"slices"

	"github.com/gin-gonic/gin"
)

var permMapper = make(map[string][2]TPack)

type TPack struct {
	AuthSerial   string
	primaryPerm  []string
	permutedPerm []string
}

func (tp *TPack) permut() error {
	permuted := make([]string, len(tp.primaryPerm))
	copy(permuted, tp.primaryPerm)

	if err := SecureShuffle(permuted); err != nil {
		return err
	}

	tp.permutedPerm = permuted
	return nil
}

func SecureShuffle[T any](a []T) error {
	n := len(a)
	for i := n - 1; i > 0; i-- {
		r, err := rand.Int(rand.Reader, big.NewInt(int64(i+1)))
		if err != nil {
			return err
		}
		j := int(r.Int64())
		a[i], a[j] = a[j], a[i]
	}
	return nil
}

func (t *TPack) getMapping() map[uint8]uint8 {
	mapa := make(map[uint8]uint8)
	for i, perm := range t.permutedPerm {
		mapa[uint8(i)] = uint8(slices.Index(t.primaryPerm, perm))
	}
	fmt.Printf("mapa := %v\n", mapa)
	return mapa
}

func popDocuments() [2]TPack {
	var eaPack [2]TPack
	var err error
	if eaPack[0].AuthSerial, eaPack[0].primaryPerm, err = helpers.ProcessServerData(helpers.FetchDataFromServers()); err != nil {
		panic(err)
	}
	if eaPack[1].AuthSerial, eaPack[1].primaryPerm, err = helpers.ProcessServerData(helpers.FetchDataFromServers()); err != nil {
		panic(err)
	}
	if eaPack[0].permut() != nil {
		panic(err)
	}
	if eaPack[1].permut() != nil {
		panic(err)
	}

	return eaPack
}

func LinkPackToHashReturnPermuted(c *gin.Context) {
	based := c.Request.URL.Query().Get("sha")

	shab, err := base64.URLEncoding.DecodeString(based)
	sha := string(shab)
	if err != nil {
		panic(err)
	}

	packs := [2]golangShared.EaPack{
		{
			AuthSerial: string(permMapper[sha][0].AuthSerial),
			VoteCodes:  permMapper[sha][0].permutedPerm,
		},
		{
			AuthSerial: string(permMapper[sha][1].AuthSerial),
			VoteCodes:  permMapper[sha][1].permutedPerm,
		},
	}
	fmt.Printf("sha = %s\n", based)

	c.JSON(200, packs)
}

type Body struct {
	Xml string `xml:"xml"`
}

type Pack struct {
	Mapping    map[uint8]uint8 `json:"mapping"`
	AuthSerial string          `json:"authSerial"`
}

func GetPermutation(c *gin.Context) {
	bodyBytes, err := io.ReadAll(c.Request.Body)
	if err != nil {
		panic(err)
	}

	sha := sha512.Sum512(bodyBytes)
	if err := golangShared.VerifySign(string(bodyBytes)); err != nil {
		panic(err)
		return
	}

	doc := popDocuments()

	permMapper[string(sha[:])] = doc

	p0 := doc[0].getMapping()
	p1 := doc[1].getMapping()
	c.JSON(200, [2]Pack{
		{Mapping: p0, AuthSerial: string(doc[0].AuthSerial)},
		{Mapping: p1, AuthSerial: string(doc[1].AuthSerial)},
	})
}
