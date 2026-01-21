package main

import (
	"FakeSgx/endpoints"
	"fmt"
	"golangShared"
	"time"

	"github.com/gin-contrib/cors"
	"github.com/gin-gonic/gin"
)

// take data from trustee
func initialize() {

}

func main() {
	r := gin.Default()

	initialize()
	r.Use(cors.New(cors.Config{
		AllowAllOrigins:  true,
		AllowMethods:     []string{"GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"},
		AllowHeaders:     []string{"*"},
		ExposeHeaders:    []string{"Content-Length", "Content-Type"},
		MaxAge:           12 * time.Hour,
		AllowCredentials: false,
	}))
	r.POST("/voter", endpoints.GetPermutation)

	r.GET("/voter/codes", endpoints.GetVcFromPermCode)

	r.GET("/ea", endpoints.LinkPackToHashReturnPermuted)

	_ = r.Run(fmt.Sprintf(":%d", golangShared.SGXPort))

}
