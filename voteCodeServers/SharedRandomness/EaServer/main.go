package main

import (
	"SharedRandomness/commitments"
	"commiter/common"
	"crypto/sha512"
	"encoding/base64"
	"encoding/binary"
	"fmt"
	"golangShared/commiterStruct"
	"net/http"
	"sort"

	"github.com/gin-gonic/gin"
)

const participant = 3

func main() {
	r := gin.Default()

	var cMap map[string]commitments.Commitment = make(map[string]commitments.Commitment)
	var vMap map[string]commitments.Reveal = make(map[string]commitments.Reveal)
	r.POST("/commit", func(c *gin.Context) {
		var commit commitments.Commitment
		if err := c.ShouldBindJSON(&commit); err != nil {
			fmt.Printf("%v\n", err)
			c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
			return
		}

		fmt.Printf("%v\n", commit)
		if !commit.CheckSign() {
			println("bad sign")
			c.JSON(http.StatusBadRequest, gin.H{"error": "bad Sign"})
			return
		}

		if _, ok := cMap[commit.Id]; ok != false {
			c.JSON(http.StatusBadRequest, gin.H{"error": "commit already exists"})
			return
		}

		cMap[commit.Id] = commit
		c.Status(200)
	})

	r.GET("/isReady", func(c *gin.Context) {
		if len(cMap) >= participant {
			c.JSON(http.StatusOK, gin.H{"isOK": true})
		} else {
			c.JSON(http.StatusInternalServerError, gin.H{"isOK": false})
		}
	})

	r.GET("commit", func(c *gin.Context) {
		c.JSON(200, gin.H{
			"data": cMap,
		})
	})

	r.POST("/revealValue", func(c *gin.Context) {
		var reveal commitments.Reveal
		if err := c.ShouldBindJSON(&reveal); err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
			return
		}

		if !reveal.CheckSign() {
			c.JSON(http.StatusBadRequest, gin.H{"error": "bad Sign"})
			return
		}

		if _, ok := vMap[reveal.Id]; ok != false {
			c.JSON(http.StatusBadRequest, gin.H{"error": "reveal already exists"})
			return
		}

		vMap[reveal.Id] = reveal

		c.Status(http.StatusOK)
	})

	r.GET("/getValues", func(c *gin.Context) {
		c.JSON(200, gin.H{
			"data": vMap,
		})
	})

	r.GET("/getRevealRandomness", func(c *gin.Context) {

		if len(vMap) >= participant {
			randomness := HashMapReveal(vMap)
			if commited == false {
				err := commiterStruct.CommitSingle(common.SharedRandomness, 0, randomness)
				if err != nil {
					fmt.Printf("error while commiting := %v\n", err)
				}
				commited = true
			}
			fmt.Printf("successfull commited shared randomness %v\n", randomness)

			c.JSON(200, gin.H{
				"data": string(base64.StdEncoding.EncodeToString(randomness[:])),
			})
		}
		c.JSON(200, gin.H{
			"data": "",
		})

	})

	r.Run(":6969")
}

var commited = false

func HashMapReveal(m map[string]commitments.Reveal) [64]byte {
	keys := make([]string, 0, len(m))
	for k := range m {
		keys = append(keys, k)
	}
	sort.Strings(keys)

	h := sha512.New()
	h.Write([]byte("map[string]Reveal:v1\x00"))

	var lenBuf [8]byte
	writeBytes := func(b []byte) {
		binary.LittleEndian.PutUint64(lenBuf[:], uint64(len(b)))
		h.Write(lenBuf[:])
		h.Write(b)
	}
	writeString := func(s string) { writeBytes([]byte(s)) }

	for _, k := range keys {
		v := m[k]

		// klucz mapy (kanonicznie)
		writeString(k)

		// wartości (kanonicznie, surowe bajty + długości)
		// Dostosuj do realnych typów pól:
		writeString(v.Id)

		// jeśli Signature i Value są []byte:
		writeBytes([]byte(v.Signature))
		writeBytes([]byte(v.Value))

		// jeśli są stringami zamiast []byte, użyj writeString(...)
	}

	var out [64]byte
	copy(out[:], h.Sum(nil))
	return out
}
