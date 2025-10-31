package main

import (
	"fmt"
	"github.com/gin-gonic/gin"
	"golangShared"
	"votingServer/endpoint"
	"votingServer/initElection"
)

func main() {
	r := gin.Default()
	initElection.CreatePackages()
	r.POST(golangShared.GetVotingPackEndpoint, endpoint.GetVotingPack)

	oblivious := endpoint.ObliviousTransfer{
		ObliviousMapper: make(map[string]string),
	}
	r.GET(golangShared.GetAuthCodeEndpoint, oblivious.GetAuthCodeStartProtocol)

	_ = r.Run(fmt.Sprintf(":%d", golangShared.VotingPort))
}
