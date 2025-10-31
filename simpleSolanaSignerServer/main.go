package main

import (
	"encoding/json"
	"fmt"
	"golangShared"
	"log"
	"os"
	"simpleSolanaSignerServer/sign"
	"time"

	"github.com/gin-contrib/cors"

	"github.com/gagliardetto/solana-go"
	"github.com/gin-gonic/gin"
)

func main() {
	r := gin.Default()

	signEndpoint := sign.SignEndpoint{
		PayerKey: loadPrivateKeyFromJSON("../signer.json"),
	}

	r.Use(cors.New(cors.Config{
		AllowAllOrigins:  true,
		AllowMethods:     []string{"GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"},
		AllowHeaders:     []string{"*"},
		ExposeHeaders:    []string{"Content-Length", "Content-Type"},
		MaxAge:           12 * time.Hour,
		AllowCredentials: false,
	}))

	r.POST(golangShared.SignEndpoint, signEndpoint.Sign)

	_ = r.Run(fmt.Sprintf(":%d", golangShared.SignerPort))
}

func loadPrivateKeyFromJSON(path string) *solana.PrivateKey {
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
