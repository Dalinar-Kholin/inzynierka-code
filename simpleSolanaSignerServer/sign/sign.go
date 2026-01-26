package sign

import (
	"context"
	"encoding/base64"
	"errors"
	"fmt"
	"golangShared"
	"golangShared/ServerResponse"
	"helpers"
	"net/http"
	"votingServer/DB"

	"github.com/gagliardetto/solana-go"
	"github.com/gin-gonic/gin"
	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/bson/primitive"
)

type SignEndpoint struct {
	PayerKey *solana.PrivateKey
}

type SignRequestData struct {
	Transaction string  `json:"transaction"`
	AuthCode    *string `json:"authCode,omitempty"` // based on entropy only allowed to get ballot can get authCode, so we dont need to extra veryfiy
	AccessCode  *string `json:"accessCode,omitempty"`
}

type SignResponse struct {
	Transaction string  `json:"transaction"`
	AccessCode  *string `json:"accessCode"`
}

type ErrorResponse struct {
	Error string `json:"error"`
}

var wantedProgram = solana.MustPublicKeyFromBase58("8PuBy6uMn4SRfDDZeJeuYH6hDE9eft1t791mFdUFc5Af")

func (s *SignEndpoint) Sign(c *gin.Context) {
	var signRequestData golangShared.SignedFrontendRequest[SignRequestData]
	if err := c.ShouldBindJSON(&signRequestData); err != nil {
		c.JSON(401, gin.H{
			"error": "bad Request Body",
		})
		return
	}

	/*newAccessCode, err := Verify(c, &signRequestData.Body)
	if err != nil {
		return
	}*/

	// chcemy sprawdzic czy podpis jest poprawny --> dane są podpisane elektoronicznie przez użytkownika

	// chcemy sprawdzić czy osoba podpisująca jest uprawniona do głosowania --> jeśli jest podpis profilem zaufanym to znaczy że jest git

	// chcemy sprawdzić czy sama transakcja jest poprawna - address konta, instrukcja itd

	// nie możemy symulować transakcji i na bazie wyniku odrzucić transakcji, jako ten server nie mamy prawa nie podpisać transakcji, która ma poprawny accessCode/authCode oraz parsuje się do poprawnej transakcji

	tx, err := solana.TransactionFromBase64(signRequestData.Body.Transaction)
	if err != nil {
		c.JSON(401, gin.H{"error": err.Error()})
		return
	}

	for _, ix := range tx.Message.Instructions {
		programID := tx.Message.AccountKeys[ix.ProgramIDIndex]
		if !programID.Equals(wantedProgram) {
			return
		}
	}

	_, _ = tx.PartialSign(func(pub solana.PublicKey) *solana.PrivateKey { return s.PayerKey })
	res, _ := tx.MarshalBinary()

	ServerResponse.ResponseWithSign(c, http.StatusOK, signRequestData,
		SignResponse{
			Transaction: base64.StdEncoding.EncodeToString(res),
			AccessCode:  nil, //newAccessCode,
		})
}

func Verify(c *gin.Context, signRequestData *SignRequestData) (*string, error) {
	var authPack golangShared.AuthPackage
	sec := helpers.SecureRandomString()
	tmp := string(sec[:])
	newAccessCode := &tmp
	var filter bson.D
	var bin primitive.Binary
	if !golangShared.IsNullOrEmpty(signRequestData.AuthCode) {
		bin = primitive.Binary{Subtype: 0x00, Data: []byte(*signRequestData.AuthCode)}
		fmt.Printf("auth COde := %s\n", *signRequestData.AuthCode)
		filter = bson.D{{"authCode.code", bin}}
		if err := DB.GetDataBase("inz", DB.AuthCollection).FindOne(context.Background(), filter).Decode(&authPack); err != nil {
			ServerResponse.ResponseWithSign(c, http.StatusUnauthorized, signRequestData, ErrorResponse{
				Error: "cant find auth package/check spelling",
			})
			return nil, err
		}
		/*jsoned, _ := json.Marshal(signRequestData) // parsuejy 2 razy do jsona na razie ale nie mam siły tego teraz zmieniać
		err := StoreClient.Client(StoreClient.RequestBody{
			AuthSerial: nil,
			AuthCode:   signRequestData.AuthCode,
			Data:       string(jsoned),
		})
		if err != nil {
			panic(err)
		}*/
	} else if !golangShared.IsNullOrEmpty(signRequestData.AccessCode) {
		bin = primitive.Binary{Subtype: 0x00, Data: []byte(*signRequestData.AccessCode)}
		filter = bson.D{{"authCode.accessCode", bin}}

		if err := DB.GetDataBase("inz", DB.AuthCollection).FindOne(context.Background(), filter).Decode(&authPack); err != nil {
			ServerResponse.ResponseWithSign(c, http.StatusUnauthorized, signRequestData, ErrorResponse{
				Error: "cant find auth package/check spelling",
			})
			return nil, err
		}
		/*		jsoned, _ := json.Marshal(signRequestData) // parsuejy 2 razy do jsona na razie ale nie mam siły tego teraz zmieniać
				err := StoreClient.Client(StoreClient.RequestBody{
					AuthSerial: nil,
					AuthCode:   signRequestData.AuthCode,
					Data:       string(jsoned),
				})
				if err != nil {
					panic(err)
				}*/
	} else {
		c.JSON(401, gin.H{"error": "bad Request Body"})
		return nil, errors.New("bad Request")
	}

	for i := range authPack.AuthCode {
		x := &authPack.AuthCode[i]
		if x.AccessCode != nil && x.AccessCode.Equal(bin) {
			if x.SignStatus != golangShared.ACTUAL {
				ServerResponse.ResponseWithSign(c, http.StatusUnauthorized, signRequestData, ErrorResponse{
					Error: "this auth code is already used, check for access code",
				})
				return nil, errors.New("this auth code is already used, check for access code")
			}
			x.AccessCode = &primitive.Binary{Subtype: 0x00, Data: []byte(*newAccessCode)}
			return nil, errors.New("this auth code is already used, check for access code")
		}

		if x.Code[0].Equal(bin) || x.Code[0].Equal(bin) {
			if x.SignStatus != golangShared.UNUSED {
				ServerResponse.ResponseWithSign(c, http.StatusUnauthorized, signRequestData, ErrorResponse{
					Error: "this auth code is already used, check for access code",
				})
				return nil, errors.New("this auth code is already used, check for access code")
			}
			x.AccessCode = &primitive.Binary{Subtype: 0x00, Data: []byte(*newAccessCode)}
			x.SignStatus = golangShared.ACTUAL
			break
		}
	}

	if _, err := DB.GetDataBase("inz", DB.AuthCollection).ReplaceOne(
		context.Background(),
		filter,
		authPack,
	); err != nil {
		ServerResponse.ResponseWithSign(c, http.StatusInternalServerError, signRequestData, ErrorResponse{
			Error: "internal server error",
		})
		return nil, errors.New("this auth code is already used, check for access code")
	}
	return newAccessCode, nil
}
