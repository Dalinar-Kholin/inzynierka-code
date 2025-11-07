package main

import (
	"fmt"
	"golangShared"
	"time"
	"votingServer/endpoint"
	"votingServer/initElection"
	"votingServer/obliviousTransfer"

	"github.com/gin-contrib/cors"
	"github.com/gin-gonic/gin"
)

func main() {
	r := gin.Default()
	initElection.CreatePackages()
	r.Use(cors.New(cors.Config{
		AllowAllOrigins:  true, // uwaga: nie z credentials
		AllowMethods:     []string{"GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"},
		AllowHeaders:     []string{"*"},
		ExposeHeaders:    []string{"Content-Length", "Content-Type"},
		MaxAge:           12 * time.Hour,
		AllowCredentials: false,
	}))

	r.POST(golangShared.GetVotingPackEndpoint, endpoint.GetVotingPack)

	r.POST(golangShared.GetAuthCodeInitEndpoint, obliviousTransfer.InitProtocol)
	r.POST(golangShared.GetAuthCodeEndpoint, obliviousTransfer.Encrypt)

	_ = r.Run(fmt.Sprintf(":%d", golangShared.VotingPort))
}
