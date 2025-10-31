package voting

import (
	"encoding/json"
	"net/http"
	"simpleSolanaSignerServer/DB"

	"github.com/gin-gonic/gin"
)

/*
wykorzystamy oblivious transfer na bazie Deffe-Helman
protokół to
server posiada 2 sekrety które chce przekazać
user rozpoczyna komunikacje, odstaje wartość X
user wyznacza 2 klucze publiczne gdzie X = pubK1 + pubK2
stąd potrafi on wyznaczyć klucz prywatny tylko do 1
server każdy serkret szyfruje osobnym kluczem i przesyła userowi
user odszyfrowuje jedną z wiadomości do której zna klucz
koniec konwersacji
*/

var obliviousMapper map[string]string = make(map[string]string)

type ObliviousTransferBody struct {
	AuthSerial string `json:"authSerial"`
}

type ObliviousTransfer struct {
	ObliviousMapper map[string]string
}

func (s *ObliviousTransfer) CommitAuthPack() {
	//TODO implement me
	panic("implement me")
}

func (s *ObliviousTransfer) CommitVotePack() {
	//TODO implement me
	panic("implement me")
}

func (s *ObliviousTransfer) GetAuthCodeStartProtocol(c *gin.Context) {
	var sot ObliviousTransferBody
	if err := json.NewDecoder(c.Request.Body).Decode(&sot); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
	}
	/* Todo : dodać oblivious transfer na razie przekazujemy jawnie
	imo można to zrobić jakoś za pomocą web socketów zamiast otwierać wiele endpointów*/
	DB.GetDataBase("inz", DB.AuthCollection)

	//todo: sprawdzenie czy nie ma
	// s.Commit(sot.AuthSerial, "nice")

}

func GetAuthCodeStep2(c *gin.Context) {

}
