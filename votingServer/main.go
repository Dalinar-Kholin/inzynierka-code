package main

import (
	"bytes"
	"compress/gzip"
	"context"
	"crypto/ed25519"
	"crypto/x509"
	"encoding/pem"
	"fmt"
	"golangShared"
	"io"
	"os"
	"strings"
	"time"
	"votingServer/commitment"
	"votingServer/endpoint"
	"votingServer/endpoint/AcceptVote"
	"votingServer/helper"
	"votingServer/initElection"

	"github.com/gagliardetto/solana-go"
	"github.com/gagliardetto/solana-go/rpc"
	"github.com/gin-contrib/cors"
	"github.com/gin-gonic/gin"
)

func main() {
	r := gin.Default()
	// time.Sleep(5 * time.Second)
	pemBytes, err := os.ReadFile("../ed25519_pub.pem")
	if err != nil {
		panic(err)
	}

	if err := commitment.CommitSignKey(string(pemBytes)); err != nil {

	}

	stringed := string(pemBytes)
	res := strings.Replace(stringed, "-----BEGIN PUBLIC KEY-----", "", -1)
	res = strings.Replace(res, "-----END PUBLIC KEY-----", "", -1)
	res = strings.TrimSpace(res)
	endpoint.ServerPubKey = res
	fmt.Printf("public key string := %s\n", res)

	initElection.CreatePackages()
	r.Use(cors.New(cors.Config{
		AllowAllOrigins:  true,
		AllowMethods:     []string{"GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"},
		AllowHeaders:     []string{"*"},
		ExposeHeaders:    []string{"Content-Length", "Content-Type"},
		MaxAge:           12 * time.Hour,
		AllowCredentials: false,
	}))

	payer := WalletFromPrivateKey(golangShared.LoadPrivateKeyFromJSON("../signer.json"))
	helper.FeePayer = payer

	pKey, err := loadEd25519PrivateKey("../ed25519_key.pem")
	if err != nil {
		panic(err)
	}
	helper.SignKey = pKey

	r.POST(golangShared.GetVotingPackEndpoint, endpoint.GetVotingPack)
	r.POST(golangShared.GetVoteCodesEndpoint, endpoint.GetVoteCodes)
	r.POST(golangShared.GetAuthCodeInitEndpoint, endpoint.GetAuthCodeInit)
	r.POST(golangShared.GetAuthCodeEndpoint, endpoint.GetAuthCodeFinal)
	r.POST(golangShared.AcceptVoteEndpoint, AcceptVote.AcceptVote)

	r.POST("/test", func(c *gin.Context) {
		var body Body
		if err := c.ShouldBindJSON(&body); err != nil {
			panic(err)
		}
		rp := rpc.New("http://127.0.0.1:8899")

		pda, _, err := solana.FindProgramAddress(
			[][]byte{[]byte("commitVote"), []byte(body.AuthCode[:32]), []byte(body.AuthCode[32:])},
			helper.ProgramID,
		)
		if err != nil {
			panic(err)
		}

		acc, err := rp.GetAccountInfo(context.Background(), pda)
		if err != nil {
			panic(err)
		}
		voteAnchorModel, err := helper.DecodeVoteAnchor(acc.Bytes())
		if err != nil {
			panic(err)
		}

		fmt.Printf("bytes := %v\n", voteAnchorModel.VoterSign[0:10])

		st, err := DecompressGzipToString(voteAnchorModel.VoterSign[:])
		if err != nil {
			panic(err)
		}
		fmt.Printf("udcoded Data := %s\n", st)
	})

	_ = r.Run(fmt.Sprintf(":%d", golangShared.VotingPort))
}

func DecompressGzipToString(input []byte) (string, error) {

	fmt.Printf("bytes := %v\n", len(input))
	r, err := gzip.NewReader(bytes.NewReader(input))
	if err != nil {
		return "", err
	}
	defer r.Close()

	var buf bytes.Buffer
	if _, err := io.Copy(&buf, r); err != nil {
		return "", err
	}

	return buf.String(), nil
}

type Body struct {
	Sign       string `json:"sign"`
	VoteSerial string `json:"voteSerial"`
	AuthCode   string `json:"authCode"`
} // server nie przechowuje <voteSerial, authSerial>

func WalletFromPrivateKey(pk *solana.PrivateKey) *solana.Wallet {
	return &solana.Wallet{PrivateKey: *pk}
}

func loadEd25519PrivateKey(path string) (ed25519.PrivateKey, error) {
	pemBytes, err := os.ReadFile(path)
	if err != nil {
		return nil, fmt.Errorf("read file: %w", err)
	}

	block, _ := pem.Decode(pemBytes)
	if block == nil {
		return nil, fmt.Errorf("no PEM block found")
	}
	if block.Type != "PRIVATE KEY" {
		return nil, fmt.Errorf("unexpected PEM type: %s", block.Type)
	}

	// OpenSSL genpkey ED25519 → PKCS#8
	parsedKey, err := x509.ParsePKCS8PrivateKey(block.Bytes)
	if err != nil {
		return nil, fmt.Errorf("parse PKCS#8: %w", err)
	}

	priv, ok := parsedKey.(ed25519.PrivateKey)
	if !ok {
		return nil, fmt.Errorf("not an Ed25519 private key")
	}

	return priv, nil
}
