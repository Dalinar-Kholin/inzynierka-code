package helper

import (
	"context"
	"fmt"

	"github.com/gagliardetto/solana-go"
	"github.com/gagliardetto/solana-go/rpc"
)

func SendInterpretVote(
	ctx context.Context,
	authCode []byte, // 64 bytes
	status uint8,
) (string, error) {

	// 1) Walidacja długości
	if len(authCode) != 64 {
		return "", fmt.Errorf("invalid lengths: authCode=%d", len(authCode))
	}

	// 2) Wylicz PDA konta vote z auth_code
	votePDA, _, err := findVotePDA(authCode)
	if err != nil {
		return "", fmt.Errorf("find PDA: %w", err)
	}

	// 3) Zbuduj dane instrukcji: discriminator + Borsh Vec<u8>... w kolejności argumentów
	data := make([]byte, 0, 8+4+64+4+16+4+4+8+4+64)
	data = append(data, methodDiscriminator("interpret_card_vote")...)
	data = borshAppendVec(data, authCode)
	data = append(data, status)

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
