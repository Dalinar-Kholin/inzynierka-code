package main

import (
	"FakeSgx/endpoints"
	"fmt"
	"golangShared"
	"helpers"
	"net/http"
	"time"

	"github.com/gin-contrib/cors"
	"github.com/gin-gonic/gin"
)

// take data from trustee
func initialize() {
	endpoints.LoadData()
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

	r.GET("/ea", endpoints.LinkPackToHashReturnPermuted)

	r.GET("/xd", func(context *gin.Context) {
		as, vc, err := helpers.ProcessServerData(helpers.FetchDataFromServers())
		if err != nil {
			panic(err)
		}

		fmt.Printf("as:= %v\n", as)
		fmt.Printf("vc:= %v\n", vc)
		context.JSON(http.StatusOK, as)
	})

	_ = r.Run(fmt.Sprintf(":%d", golangShared.SGXPort))

}
