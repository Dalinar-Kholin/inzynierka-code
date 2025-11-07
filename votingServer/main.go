package main

import (
	"fmt"
	"golangShared"
	"time"
	"votingServer/endpoint"
	"votingServer/initElection"

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

	r.POST(golangShared.GetVotingPackEndpoint, endpoint.GetVotingPack)

	r.POST(golangShared.GetAuthCodeInitEndpoint, endpoint.GetAuthCodeInit)
	r.POST(golangShared.GetAuthCodeEndpoint, endpoint.GetAuthCodeFinal)

	_ = r.Run(fmt.Sprintf(":%d", golangShared.VotingPort))
}
