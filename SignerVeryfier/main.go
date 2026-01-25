package main

import (
	"fmt"
	"golangShared"
	"net/http"
	"os"
	"os/exec"
	"time"

	"github.com/gin-contrib/cors"
	"github.com/gin-gonic/gin"
)

func main() {
	r := gin.Default()

	r.POST(golangShared.VerifySignKeyEndpoint, func(c *gin.Context) {
		var body golangShared.VerifyBody
		if err := c.ShouldBindJSON(&body); err != nil {
			panic(err)
		}

		tmpFile, err := os.CreateTemp("", "dss-sign-*.xml")
		if err != nil {
			c.Status(500)
			return
		}
		tmpName := tmpFile.Name()
		// Sprzątanie po sobie po zakończeniu funkcji
		defer os.Remove(tmpName)

		// 2. Zapisujemy dane do pliku
		if _, err := tmpFile.WriteString(body.Sign); err != nil {
			tmpFile.Close()
			c.Status(500)
			return
		}
		if err := tmpFile.Close(); err != nil {
			c.Status(500)
			return
		}

		jarPath := "./dss-cli-validator/target/dss-cli-validator-1.0-SNAPSHOT.jar"

		cmd := exec.Command(
			"java",
			"-jar",
			jarPath,
			tmpName,
			"../certificate.pem",
		)

		cmd.Stdout = os.Stdout
		cmd.Stderr = os.Stderr
		cmd.Stdin = os.Stdin

		if err := cmd.Run(); err != nil {
			if cmd.ProcessState != nil {
				code := cmd.ProcessState.ExitCode()

				fmt.Printf("exit code := %v\n", code)

				if code == 69 {
					c.Status(http.StatusOK)
					return
				}
				c.Status(http.StatusBadRequest)
				fmt.Printf("exit code: %d\n", code)
			} else {
				fmt.Printf("failed to start process: %v\n", err)
				c.Status(http.StatusInternalServerError)
			}
		}

	})

	r.Use(cors.New(cors.Config{
		AllowAllOrigins:  true,
		AllowMethods:     []string{"GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"},
		AllowHeaders:     []string{"*"},
		ExposeHeaders:    []string{"Content-Length", "Content-Type"},
		MaxAge:           12 * time.Hour,
		AllowCredentials: false,
	}))

	_ = r.Run(fmt.Sprintf(":%d", golangShared.VerifierPort))

}
