package sign

import (
	"encoding/base64"
	"encoding/json"
	"log"

	"github.com/gagliardetto/solana-go"
	"github.com/gin-gonic/gin"
)

type SignEndpoint struct {
	PayerKey *solana.PrivateKey
}

type SignRequestData struct {
	Transaction string `json:"transaction"`
	Signature   string `json:"signature"`
}

func (s *SignEndpoint) Sign(c *gin.Context) {
	var signRequestData SignRequestData
	decoder := json.NewDecoder(c.Request.Body)
	err := decoder.Decode(&signRequestData)
	if err != nil {
		c.JSON(200, gin.H{
			"error": "bad Request Body",
		})
		return
	}
	// chcemy sprawdzic czy podpis jest poprawny --> dane są podpisane elektoronicznie przez użytkownika

	// chcemy sprawdzić czy osoba podpisująca jest uprawniona do głosowania

	// done nie musimy sprawdzać czy podpis transakcji jest poprawny bo jeżeli nie jest to walidatorzy nie przepuszczą transakcji

	// chcemy sprawdzić czy sama transakcja jest poprawna - address konta, instrukcja itd

	// symulowanie transakcji aby sprawdzić czy instrukcje sa poprawne aby nie podpisywać transakcji która i tak sie wysypie
	tx, err := solana.TransactionFromBase64(signRequestData.Transaction)
	if err != nil {
		log.Fatal(err)
	}

	_, _ = tx.PartialSign(func(pub solana.PublicKey) *solana.PrivateKey {

		return s.PayerKey
	})
	res, _ := tx.MarshalBinary()
	c.JSON(
		200, gin.H{"transaction": base64.URLEncoding.EncodeToString(res)},
	)

}
