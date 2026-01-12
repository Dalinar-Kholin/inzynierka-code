package helper

import (
	"context"
	"crypto/sha256"
	"encoding/binary"
	"errors"
	"fmt"

	"github.com/gagliardetto/solana-go"
	"github.com/gagliardetto/solana-go/rpc"
)

func methodDiscriminator(name string) []byte {
	sum := sha256.Sum256([]byte("global:" + name))
	return sum[:8]
}

// Borsh Vec<u8>: u32 LE (len) + bytes
func borshAppendVec(dst []byte, v []byte) []byte {
	var lenLE [4]byte
	binary.LittleEndian.PutUint32(lenLE[:], uint32(len(v)))
	dst = append(dst, lenLE[:]...)
	dst = append(dst, v...)
	return dst
}

func findVotePDA(authCode []byte) (solana.PublicKey, uint8, error) {
	if len(authCode) != 64 {
		return solana.PublicKey{}, 0, fmt.Errorf("auth_code must be 64 bytes, got %d", len(authCode))
	}
	return solana.FindProgramAddress(
		[][]byte{
			[]byte("commitVote"),
			authCode[:32],
			authCode[32:],
		},
		ProgramID,
	)
}

func SendAcceptVote(
	ctx context.Context,
	authCode []byte, // 64 bytes
	authSerial []byte, // 16
	voteSerial []byte, // 16
	serverSign []byte, // 64
) (string, error) {

	// 1) Walidacja długości
	if len(authCode) != 64 || len(authSerial) != 16 || len(voteSerial) != 16 {
		return "", fmt.Errorf("invalid lengths: authCode=%d authSerial=%d voteSerial=%d serverSign=%d",
			len(authCode), len(authSerial), len(voteSerial), len(serverSign))
	}

	// 2) Wylicz PDA konta vote z auth_code
	votePDA, _, err := findVotePDA(authCode)
	if err != nil {
		return "", fmt.Errorf("find PDA: %w", err)
	}

	// 3) Zbuduj dane instrukcji: discriminator + Borsh Vec<u8>... w kolejności argumentów
	data := make([]byte, 0, 8+4+64+4+16+4+16+4+8+4+64)
	data = append(data, methodDiscriminator("accept_vote")...)
	data = borshAppendVec(data, authCode)
	data = borshAppendVec(data, authSerial)
	data = borshAppendVec(data, voteSerial)
	data = borshAppendVec(data, serverSign)

	// 4) Instruction (tylko jedno konto: vote - writable, bez signera)
	ix := &solana.GenericInstruction{
		AccountValues: []*solana.AccountMeta{
			{PublicKey: votePDA, IsSigner: false, IsWritable: true},
		},
		ProgID:    ProgramID,
		DataBytes: data,
	}

	// 5) Zbuduj i podpisz transakcję
	client := rpc.New("http://127.0.0.1:8899")

	recent, err := client.GetLatestBlockhash(ctx, rpc.CommitmentFinalized)
	if err != nil {
		return "", fmt.Errorf("blockhash: %w", err)
	}

	tx, err := solana.NewTransaction(
		[]solana.Instruction{ix},
		recent.Value.Blockhash,
		solana.TransactionPayer(FeePayer.PublicKey()),
	)
	if err != nil {
		return "", fmt.Errorf("new tx: %w", err)
	}

	_, err = tx.Sign(func(pk solana.PublicKey) *solana.PrivateKey {
		if pk == FeePayer.PublicKey() {
			return &FeePayer.PrivateKey
		}
		return nil
	})

	if err != nil {
		return "", fmt.Errorf("sign: %w", err)
	}

	// 6) Wysłanie
	sig, err := client.SendTransaction(ctx, tx)
	if err != nil {
		return "", fmt.Errorf("send: %w", err)
	}

	return sig.String(), nil
}

type Vote struct {
	Stage      uint8
	VoteSerial [16]byte
	VoteCode   [3]byte
	AuthSerial [16]byte
	AuthCode   [64]byte
	ServerSign [64]byte
	VoterSign  []byte
	Bump       uint8
}

func DecodeVoteAnchor(data []byte) (Vote, error) {

	const total = 5177
	if len(data) != total {
		return Vote{}, fmt.Errorf("unexpected length %d, want %d", len(data), total)
	}
	var v Vote
	payload := data[8:]

	v.Stage = payload[0]

	copy(v.VoteSerial[:], payload[1:1+16])
	copy(v.VoteCode[:], payload[17:17+3])
	copy(v.AuthSerial[:], payload[20:20+16])
	copy(v.AuthCode[:], payload[36:36+64])
	copy(v.ServerSign[:], payload[100:100+64])
	length, _ := ReadSolanaU32AsInt32(payload[164 : 164+4])
	v.VoterSign = make([]byte, length)
	copy(v.VoterSign[:], payload[168:168+length])
	v.Bump = payload[168+length]

	fmt.Printf("%v\n", length)
	return v, nil
}

func ReadSolanaU32AsInt32(data []byte) (int32, error) {
	if len(data) < 4 {
		return 0, errors.New("not enough bytes for u32")
	}

	// Odczytaj jako uint32 (LE)
	u := binary.LittleEndian.Uint32(data)

	return int32(u), nil
}

var (
	ProgramID = solana.MustPublicKeyFromBase58("HEowdZXRMsKjiJkvgGfSWQQHc3gGpPd4CdQ7TjMSNkpG")
	FeePayer  *solana.Wallet
	Client    *rpc.Client
	ctx       = context.Background()
)
