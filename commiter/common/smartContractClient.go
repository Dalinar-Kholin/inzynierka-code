package common

import (
	"context"
	"crypto/sha256"
	"encoding/binary"
	"strings"

	"github.com/gagliardetto/solana-go"
	"github.com/gagliardetto/solana-go/rpc"
	"github.com/gagliardetto/solana-go/rpc/jsonrpc"
)

var (
	ProgramID = solana.MustPublicKeyFromBase58("8PuBy6uMn4SRfDDZeJeuYH6hDE9eft1t791mFdUFc5Af")
	Payer     *solana.Wallet
	Client    *rpc.Client
	ctx       = context.Background()
)

func disc(method string) []byte {
	sum := sha256.Sum256([]byte("global:" + method))
	return sum[:8]
}

func borshAppendU32LE(dst []byte, v uint32) []byte {
	var buf [4]byte
	binary.LittleEndian.PutUint32(buf[:], v)
	return append(dst, buf[:]...)
}

func borshAppendString(dst []byte, s string) []byte {
	dst = borshAppendU32LE(dst, uint32(len(s)))
	return append(dst, []byte(s)...)
}

func CallCreateCommitmentPack(authSerial [16]byte, hashedData [32]byte) (solana.Signature, error) {
	seedA := []byte("createPackCommitment")
	commitmentPDA, _, err := solana.FindProgramAddress(
		[][]byte{seedA, authSerial[:]},
		ProgramID,
	)
	if err != nil {
		panic(err)
		return solana.Signature{}, err
	}

	var data []byte
	data = append(data, disc("create_commitment_pack")...)
	data = append(data, authSerial[:]...)
	data = append(data, hashedData[:]...)

	ix := &solana.GenericInstruction{
		ProgID: ProgramID,
		AccountValues: []*solana.AccountMeta{
			{PublicKey: Payer.PublicKey(), IsSigner: true, IsWritable: true},
			{PublicKey: commitmentPDA, IsSigner: false, IsWritable: true},
			{PublicKey: solana.SystemProgramID, IsSigner: false, IsWritable: false},
		},
		DataBytes: data,
	}

	sendOnce := func(commitment rpc.CommitmentType) (solana.Signature, error) {
		recent, err := Client.GetLatestBlockhash(ctx, commitment)
		if err != nil {
			panic(err)
			return solana.Signature{}, err
		}

		tx, err := solana.NewTransaction(
			[]solana.Instruction{ix},
			recent.Value.Blockhash,
			solana.TransactionPayer(Payer.PublicKey()),
		)
		if err != nil {
			panic(err)
			return solana.Signature{}, err
		}

		_, err = tx.Sign(func(pk solana.PublicKey) *solana.PrivateKey {
			if pk == Payer.PublicKey() {
				return &Payer.PrivateKey
			}
			return nil
		})
		if err != nil {
			panic(err)
			return solana.Signature{}, err
		}

		retries := uint(3)

		opts := &rpc.TransactionOpts{
			SkipPreflight:       false,
			PreflightCommitment: rpc.CommitmentConfirmed,
			MaxRetries:          &retries,
		}
		return Client.SendTransactionWithOpts(ctx, tx, *opts)
	}

	commitment := rpc.CommitmentConfirmed
	sig, err := sendOnce(commitment)
	if err != nil {
		if rpcErr, ok := err.(*jsonrpc.RPCError); ok && rpcErr != nil && strings.Contains(rpcErr.Message, "Blockhash not found") {
			return sendOnce(commitment)
		}
		panic(err)
		return solana.Signature{}, err
	}
	return sig, nil
}
