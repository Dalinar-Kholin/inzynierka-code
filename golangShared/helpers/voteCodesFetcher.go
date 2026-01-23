// helper functions for fake SGX

package helpers

import (
	"encoding/json"
	"fmt"
	"io"
	"math/big"
	"net/http"
	"sync"
	"time"
)

const (
	BasePort       = 5000
	NumServers     = 10 // number of servers
	RequestTimeout = 10 * time.Second
)

// paillier parameters (created when generating the keys)
var (
	PaillierN        = "681857855702518740704953601369673633705135695298229808586169116464264137928690445929259101132662230830731696844493369649448124756686478300425338032781377689"
	PaillierNSquared = "464930135383236868762294282050575283938809426167548765226163728200940659685431102544884065874275716634166967233262482170376770348807693876165684682131900426399828933721979124423334934940339166587941442600701425521344926937573241873018987863259105401307755464132075701586932737973059065335629721586487188866980721"
	PaillierThetaInv = "629308883988655905523753285434973609735521955413121655314869469798359331814399404413728965733664739718336549541383011258452262804433288695289430286141092560"
	PaillierDegree   = 8

	Alphabet = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM1234567890"

	// global parameters for SplitAndDistribute
	A = 10 // lenght of VS
	B = 5  // number of candidates
)

type ServerJSONResponse struct {
	Success  bool        `json:"success"`
	ServerID int         `json:"serverId"`
	Data     json.Number `json:"data"`
	Index    int         `json:"index"`
}

type ServerResponse struct {
	ServerID int
	Data     string
	Error    error
}

func FetchDataFromServers() []ServerResponse {
	responses := make([]ServerResponse, NumServers)
	var wg sync.WaitGroup

	client := &http.Client{
		Timeout: RequestTimeout,
	}

	for i := 0; i < NumServers; i++ {
		wg.Add(1)
		go func(serverID int) {
			defer wg.Done()

			port := BasePort + serverID + 1 // ports 5001-5010
			url := fmt.Sprintf("http://localhost:%d/api/data/next", port)

			resp, err := client.Get(url)
			if err != nil {
				responses[serverID] = ServerResponse{
					ServerID: serverID + 1,
					Data:     "",
					Error:    fmt.Errorf("failed to fetch from server %d: %w", serverID+1, err),
				}
				return
			}
			defer resp.Body.Close()

			body, err := io.ReadAll(resp.Body)
			if err != nil {
				responses[serverID] = ServerResponse{
					ServerID: serverID + 1,
					Data:     "",
					Error:    fmt.Errorf("failed to read response from server %d: %w", serverID+1, err),
				}
				return
			}

			if resp.StatusCode != http.StatusOK {
				responses[serverID] = ServerResponse{
					ServerID: serverID + 1,
					Data:     string(body),
					Error:    fmt.Errorf("server %d returned status %d", serverID+1, resp.StatusCode),
				}
				return
			}

			responses[serverID] = ServerResponse{
				ServerID: serverID + 1,
				Data:     string(body),
				Error:    nil,
			}
		}(i)
	}

	wg.Wait()
	return responses
}

func ProcessServerData(responses []ServerResponse) (string, []string, error) {
	partialDict := make(map[int]*big.Int)
	var errors []error

	for _, response := range responses {
		if response.Error != nil {
			errors = append(errors, response.Error)
			continue
		}

		var jsonResp ServerJSONResponse
		err := json.Unmarshal([]byte(response.Data), &jsonResp)
		if err != nil {
			errors = append(errors, fmt.Errorf("failed to parse JSON from server %d: %w", response.ServerID, err))
			continue
		}

		partialDecryption := new(big.Int)
		_, ok := partialDecryption.SetString(jsonResp.Data.String(), 10)
		if !ok {
			errors = append(errors, fmt.Errorf("failed to parse data from server %d as big integer: %s", response.ServerID, jsonResp.Data.String()))
			continue
		}

		partialDict[response.ServerID] = partialDecryption
	}

	if len(partialDict) < PaillierDegree+1 {
		if len(errors) > 0 {
			fmt.Printf("DEBUG: First error: %v\n", errors[0])
		}
		return "", nil, fmt.Errorf("not enough valid responses: got %d, need at least %d (errors: %d)", len(partialDict), PaillierDegree+1, len(errors))
	}

	decryptedMessage, err := Decrypt(partialDict)
	if err != nil {
		return "", nil, fmt.Errorf("decryption failed: %w", err)
	}

	decryptedMessageStr := Decode(decryptedMessage)
	VS, VCs := SplitAndDistribute(decryptedMessageStr)

	return VS, VCs, nil
}

func FetchAndProcess() (string, []string, error) {
	responses := FetchDataFromServers()
	return ProcessServerData(responses)
}

// Decrypt decrypts a Paillier ciphertext using partial decryptions from multiple parties
// partialDict: map of server ID to partial decryption value
// Uses hardcoded Paillier parameters
func Decrypt(partialDict map[int]*big.Int) (*big.Int, error) {
	// Parse hardcoded parameters
	n := new(big.Int)
	_, ok := n.SetString(PaillierN, 10)
	if !ok {
		return nil, fmt.Errorf("invalid PaillierN")
	}

	nSquared := new(big.Int)
	_, ok = nSquared.SetString(PaillierNSquared, 10)
	if !ok {
		return nil, fmt.Errorf("invalid PaillierNSquared")
	}

	thetaInv := new(big.Int)
	_, ok = thetaInv.SetString(PaillierThetaInv, 10)
	if !ok {
		return nil, fmt.Errorf("invalid PaillierThetaInv")
	}

	degree := PaillierDegree

	// collect partial decryptions
	partialDecryptions := make([]*big.Int, 0, degree+1)
	for i := 0; i <= degree; i++ {
		val, exists := partialDict[i+1]
		if !exists {
			return nil, fmt.Errorf("missing partial decryption for index %d", i+1)
		}
		partialDecryptions = append(partialDecryptions, val)
	}

	if len(partialDecryptions) < degree+1 {
		return nil, fmt.Errorf("not enough shares: got %d, need %d", len(partialDecryptions), degree+1)
	}

	// combine decryptions: multiply all partial decryptions mod n^2
	combinedDecryption := multList(partialDecryptions[:degree+1], nSquared)

	// temp1 = combined_decryption - 1
	temp1 := new(big.Int).Sub(combinedDecryption, big.NewInt(1))

	// check if (combined_decryption - 1) is divisible by N
	remainder := new(big.Int).Mod(temp1, n)
	if remainder.Cmp(big.NewInt(0)) != 0 {
		return nil, fmt.Errorf("combined decryption minus one is not divisible by N. " +
			"This might be caused by the fact that the ciphertext that is being decrypted, " +
			"differs between the parties")
	}

	// temp2 = temp1 / N
	temp2 := new(big.Int).Div(temp1, n)

	// temp3 = temp2 * theta_inv
	temp3 := new(big.Int).Mul(temp2, thetaInv)

	// message = temp3 mod N
	message := new(big.Int).Mod(temp3, n)

	return message, nil
}

// multList multiplies all elements in the list together
// If modulus is provided and non-zero, applies modulo operation
func multList(list []*big.Int, modulus *big.Int) *big.Int {
	result := big.NewInt(1)

	for _, element := range list {
		result.Mul(result, element)
	}

	if modulus != nil && modulus.Cmp(big.NewInt(0)) != 0 {
		result.Mod(result, modulus)
	}

	return result
}

// decode converts a BigInt into a string using the global Alphabet.
// If Alphabet is empty, it returns an empty string.
func Decode(encoded *big.Int) string {
	if encoded == nil {
		return ""
	}
	if len(Alphabet) == 0 {
		return ""
	}

	// if encoded is zero, return the first alphabet character
	if encoded.Sign() == 0 {
		return string(Alphabet[0])
	}

	baseValue := len(Alphabet)
	baseValueBig := big.NewInt(int64(baseValue))
	value := new(big.Int).Set(encoded)

	// collect bytes in reverse order
	var reversed []byte
	for value.Sign() > 0 {
		remainder := new(big.Int)
		value.QuoRem(value, baseValueBig, remainder)

		idx := int(remainder.Int64())
		if idx < 0 || idx >= baseValue {
			return ""
		}

		reversed = append(reversed, Alphabet[idx])
	}

	// reverse to get correct order
	for i, j := 0, len(reversed)-1; i < j; i, j = i+1, j-1 {
		reversed[i], reversed[j] = reversed[j], reversed[i]
	}

	return string(reversed)
}

func SplitAndDistribute(s string) (string, []string) {
	if A < 0 {
		A = 0
	}
	if B <= 0 {
		return s, nil
	}

	if len(s) < A {
		A = len(s)
	}

	prefix := s[:A]
	buckets := make([]string, B)

	for i, ch := range s[A:] {
		idx := i % B
		buckets[idx] += string(ch)
	}

	return prefix, buckets
}
