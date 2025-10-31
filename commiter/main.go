package main

import (
	"commiter/common"
	"commiter/endpoint"
	"crypto/ecdh"
	"crypto/x509"
	"encoding/json"
	"encoding/pem"
	"fmt"
	"github.com/gagliardetto/solana-go"
	"github.com/gagliardetto/solana-go/rpc"
	"github.com/gin-contrib/cors"
	"github.com/gin-gonic/gin"
	"golangShared"
	"log"
	"os"
	"time"
)

/*
na początku zróbmy tak że server będzie wysyłał zapytanie
addAuthPackage -> a nasz server będzie budował z danych drzewo merkele
po czy po zapytaniu commitAuthPackage będzie commitowany do blockchainu
korzeń tego drzewa
*/
func main() {
	r := gin.Default()

	k1, k2 := parseX25519()
	fmt.Printf("k1:%x k2:%x\n", k1, k2)
	common.SetKeys(k1, k2)

	payer := WalletFromPrivateKey(loadPrivateKeyFromJSON("../signer.json"))
	common.Payer = payer

	client := rpc.New("http://127.0.0.1:8899")
	common.Client = client

	r.Use(cors.New(cors.Config{
		AllowAllOrigins:  true, // uwaga: nie z credentials
		AllowMethods:     []string{"GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"},
		AllowHeaders:     []string{"*"},
		ExposeHeaders:    []string{"Content-Length", "Content-Type"},
		MaxAge:           12 * time.Hour,
		AllowCredentials: false,
	}))

	r.POST(golangShared.AddCommitPackEndpoint, endpoint.AddAuthPackage)
	r.GET(golangShared.FinalCommitEndpoint, endpoint.FinalCommit)

	_ = r.Run(fmt.Sprintf(":%d", golangShared.CommiterPort))
}

func mustReadFile(p string) []byte {
	b, err := os.ReadFile(p)
	if err != nil {
		panic(err)
	}
	return b
}

func parseX25519() (*ecdh.PrivateKey, *ecdh.PublicKey) {
	// --- PRIVATE ---
	privPem := mustReadFile("../x25519_sk.pem")
	block, _ := pem.Decode(privPem)
	if block == nil || block.Type != "PRIVATE KEY" {
		panic("bad PKCS#8 PEM")
	}
	keyAny, err := x509.ParsePKCS8PrivateKey(block.Bytes)
	if err != nil {
		panic(err)
	}

	// --- PUBLIC ---
	pubPem := mustReadFile("../x25519_pk.pem")
	pb, _ := pem.Decode(pubPem)
	if pb == nil || pb.Type != "PUBLIC KEY" {
		panic("bad PKIX PEM")
	}
	pubAny, err := x509.ParsePKIXPublicKey(pb.Bytes)
	if err != nil {
		panic(err)
	}

	return keyAny.(*ecdh.PrivateKey), pubAny.(*ecdh.PublicKey)
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

func WalletFromPrivateKey(pk *solana.PrivateKey) *solana.Wallet {
	return &solana.Wallet{PrivateKey: *pk}
}
