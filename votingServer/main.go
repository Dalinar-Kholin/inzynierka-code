package main

import (
	"fmt"
	"golangShared"
	"time"
	"votingServer/endpoint"
	"votingServer/endpoint/AcceptVote"
	"votingServer/initElection"

	"github.com/gagliardetto/solana-go"
	"github.com/gin-contrib/cors"
	"github.com/gin-gonic/gin"
)

func main() {
	r := gin.Default()
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
	AcceptVote.FeePayer = payer

	r.POST(golangShared.GetVotingPackEndpoint, endpoint.GetVotingPack)
	r.POST(golangShared.GetVoteCodesEndpoint, endpoint.GetVoteCodes)
	r.POST(golangShared.GetAuthCodeInitEndpoint, endpoint.GetAuthCodeInit)
	r.POST(golangShared.GetAuthCodeEndpoint, endpoint.GetAuthCodeFinal)
	r.POST(golangShared.AcceptVoteEndpoint, AcceptVote.AcceptVote)

	_ = r.Run(fmt.Sprintf(":%d", golangShared.VotingPort))
}

func WalletFromPrivateKey(pk *solana.PrivateKey) *solana.Wallet {
	return &solana.Wallet{PrivateKey: *pk}
}
