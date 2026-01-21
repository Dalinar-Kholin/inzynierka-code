package endpoints

import (
	"crypto/sha512"
	"encoding/base64"
	"fmt"
	"golangShared"
	"io"
	"slices"

	"github.com/gin-gonic/gin"
	"go.mongodb.org/mongo-driver/bson/primitive"
)

type trusteePack struct {
	primaryPerm  []string
	permutedPerm []string
}

var permMapper = make(map[string][2]TPack)

var packOne = &trusteePack{
	primaryPerm:  []string{"ala", "bob", "cat", "def"},
	permutedPerm: []string{"def", "bob", "cat", "ala"},
}

var packTwo = &trusteePack{
	primaryPerm:  []string{"ada", "bim", "cel", "dan"},
	permutedPerm: []string{"bim", "dan", "ada", "cel"},
}

type TPack struct {
	_id          primitive.ObjectID `bson:"_id"`
	AuthSerial   primitive.Binary   `bson:"authSerial" json:"authSerial"`
	primaryPerm  []string
	permutedPerm []string
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
	return [2]TPack{{
		AuthSerial:   primitive.Binary{Subtype: 0x00, Data: []byte("7f7e3f7f-382b-479f-b389-762ec80b835c")},
		primaryPerm:  packOne.primaryPerm,
		permutedPerm: packOne.permutedPerm,
	}, {
		AuthSerial:   primitive.Binary{Subtype: 0x00, Data: []byte("3255d730-b451-4627-ac69-15eeb43d3b3a")},
		primaryPerm:  packTwo.primaryPerm,
		permutedPerm: packTwo.permutedPerm,
	}}
}

var codeMapper = make(map[string][2]golangShared.EaPack)

func GetVcFromPermCode(c *gin.Context) {
	perm := c.Request.URL.Query().Get("perm")
	fmt.Printf("mapper: %v\n", codeMapper)
	c.JSON(200, codeMapper[perm])
}

func LinkPackToHashReturnPermuted(c *gin.Context) {
	based := c.Request.URL.Query().Get("sha")
	perm := c.Request.URL.Query().Get("perm")

	shab, err := base64.URLEncoding.DecodeString(based)
	sha := string(shab)
	if err != nil {
		panic(err)
	}

	packs := [2]golangShared.EaPack{
		{
			AuthSerial: string(permMapper[sha][0].AuthSerial.Data),
			VoteCodes:  permMapper[sha][0].permutedPerm,
		},
		{
			AuthSerial: string(permMapper[sha][1].AuthSerial.Data),
			VoteCodes:  permMapper[sha][1].permutedPerm,
		},
	}
	fmt.Printf("sha = %s\n", based)

	codeMapper[perm] = packs
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
	fmt.Printf("sha := %x\n", string(sha[:]))

	if err := golangShared.VerifySign(string(bodyBytes)); err != nil {
		panic(err)
		return
	}

	doc := popDocuments()

	permMapper[string(sha[:])] = doc

	p0 := doc[0].getMapping()
	p1 := doc[1].getMapping()
	c.JSON(200, [2]Pack{
		{Mapping: p0, AuthSerial: string(doc[0].AuthSerial.Data)},
		{Mapping: p1, AuthSerial: string(doc[1].AuthSerial.Data)},
	})
}
