package main

import (
	"crypto/sha256"
	"crypto/x509"
	"encoding/base64"
	"encoding/pem"
	"encoding/xml"
	"fmt"
	"golangShared"
	"inz/SignerVeryfier/verify"
	"log"
	"os"
	"strings"
	"time"

	"github.com/gin-contrib/cors"
	"github.com/gin-gonic/gin"
)

type Body struct {
	Document string `json:"document"`
}

func main() {
	r := gin.Default()

	r.Use(cors.New(cors.Config{
		AllowAllOrigins:  true,
		AllowMethods:     []string{"GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"},
		AllowHeaders:     []string{"*"},
		ExposeHeaders:    []string{"Content-Length", "Content-Type"},
		MaxAge:           12 * time.Hour,
		AllowCredentials: false,
	}))

	r.POST("/verify", func(c *gin.Context) {
		var body Body

		if c.ShouldBindJSON(&body) != nil {
			panic("Invalid Body")
		}

		back := []byte(body.Document)
		data, err := os.ReadFile("../workingFile/proper.xml")
		if err != nil {
			panic(err)
		}

		if len([]byte(body.Document)) != len(data) {
			panic("bad lebn")
		}
		for x := range len([]byte(body.Document)) {
			if data[x] != back[x] {
				fmt.Printf("\n%s\n", data[x-8:x+8])
				fmt.Printf("%s\n", back[x-8:x+8])
				fmt.Printf("\n\n%02X %02X\n", data[x], back[x])
				panic(x)
			}
		}

		fileSum := sha256.Sum256(data)
		s := sha256.Sum256([]byte(body.Document))

		if s != fileSum {
			panic("????")
		}

		var v verify.Vote
		if err := xml.Unmarshal([]byte(body.Document), &v); err != nil {
			panic(err)
		}

		b64Cert := strings.TrimSpace(v.Signature.KeyInfo.X509Data.X509Certificates[0].Data)

		// DER z base64
		der, err := base64.StdEncoding.DecodeString(b64Cert)
		if err != nil {
			log.Fatal(err)
		}

		// Parse X.509
		cert, err := x509.ParseCertificate(der)
		if err != nil {
			log.Fatal(err)
		}

		// Zapis klucza publicznego do PEM
		if err := WritePublicKeyPEM(cert, "pubkey.pem"); err != nil {
			log.Fatal(err)
		}

		_, err = verify.VerifyVoteSignature([]byte(body.Document))
		if err != nil {
			panic(err)
		}
		fmt.Println("VoteSerial:", v.VoteSerial)

		fmt.Println("Signer cert (base64):", v.Signature.KeyInfo.X509Data.X509Certificates[0].Data)
	})

	r.Run(fmt.Sprintf(":%d", golangShared.VerifierPort))
}

func WritePublicKeyPEM(cert *x509.Certificate, path string) error {
	// cert.PublicKey ma typ *rsa.PublicKey (w Twoim przypadku)
	derBytes, err := x509.MarshalPKIXPublicKey(cert.PublicKey)
	if err != nil {
		return err
	}

	pemBlock := &pem.Block{
		Type:  "PUBLIC KEY",
		Bytes: derBytes,
	}

	f, err := os.Create(path)
	if err != nil {
		return err
	}
	defer f.Close()

	if err := pem.Encode(f, pemBlock); err != nil {
		return err
	}
	return nil
}
