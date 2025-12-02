package main

import (
	"crypto/sha512"
	"encoding/hex"
	"errors"
	"fmt"
	"golangShared"
	"inz/Storer/StoreClient"
	"net/http"
	"os"
	"path/filepath"
	"time"

	"github.com/gin-contrib/cors"
	"github.com/gin-gonic/gin"
)

type DataToStore struct {
	Path string `json:"path"`
	Data string `json:"data"`
}

var messages = make(chan DataToStore, 64) // dostosować do pamięci kontenera

func StoreData(store DataToStore) {
	name := sha512.Sum512([]byte(store.Data))
	cwd, _ := os.Getwd()
	newFileFilepath := filepath.Join(cwd, store.Path, fmt.Sprintf("%s.json", hex.EncodeToString(name[:])))
	err := os.WriteFile(newFileFilepath, []byte(store.Data), 0400)
	if err != nil {
		panic(err)
	}
}

const (
	AuthSerial = "AuthSerial"
	AuthCode   = "AuthCode"
)

func main() {
	r := gin.Default()

	go func() {
		err := os.Mkdir(AuthSerial, 700)
		if err != nil && !errors.Is(err, os.ErrExist) {
			panic(err)
			return
		}
		err = os.Mkdir(AuthCode, 700)
		if err != nil && !errors.Is(err, os.ErrExist) {
			panic(err)
			return
		}
		for {
			StoreData(<-messages)
		}
	}()

	r.Use(cors.New(cors.Config{
		AllowAllOrigins:  true,
		AllowMethods:     []string{"GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"},
		AllowHeaders:     []string{"*"},
		ExposeHeaders:    []string{"Content-Length", "Content-Type"},
		MaxAge:           12 * time.Hour,
		AllowCredentials: false,
	}))

	r.POST(golangShared.StorerEndpoint, func(c *gin.Context) {
		var body StoreClient.RequestBody
		if err := c.ShouldBindJSON(&body); err != nil {
			c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		}
		if !golangShared.IsNullOrEmpty(body.AuthSerial) {
			messages <- DataToStore{
				Path: AuthSerial,
				Data: body.Data,
			}
			c.Status(200)
			return
		}
		if !golangShared.IsNullOrEmpty(body.AuthCode) {
			messages <- DataToStore{
				Path: AuthCode,
				Data: body.Data,
			}
			c.Status(200)
			return
		}
		c.JSON(http.StatusBadRequest, gin.H{"error": "where identification"})
	})

	_ = r.Run(fmt.Sprintf(":%d", golangShared.StorerPort))
}
