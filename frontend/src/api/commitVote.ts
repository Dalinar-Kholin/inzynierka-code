import {PublicKey, Transaction} from "@solana/web3.js";
import * as anchor from "@coral-xyz/anchor";
import SignTransaction from "./signTransaction.ts";
import type {Counter} from "../counter.ts";
import type {AnchorProvider, Program} from "@coral-xyz/anchor";

interface ICommitVote {
    sign: string;
    authCode: string;
    program: Program<Counter>;
    provider: AnchorProvider;
}

export default async function commitVote({ sign, authCode, program, provider } : ICommitVote){
    const enc = new TextEncoder();
    const authU8 = enc.encode(authCode);

    if (authU8.length !== 64) throw new Error(`authCode must be 64 bytes, got ${authU8.length}`);

    const payerPubkey = new PublicKey("6zuVDoqf3KZmAWgDaqQK1K7XkmXnDyyPpCJneAYuyky1");

    const messageSeedPrefix = anchor.utils.bytes.utf8.encode("commitVote");

    const [messagePda] = PublicKey.findProgramAddressSync(
        [messageSeedPrefix, Buffer.from(authU8.slice(0, 32)), Buffer.from(authU8.slice(32, 64))],
        program.programId
    );

    const ix = await program.methods
        .commitVote(Buffer.from(authU8), Buffer.from(sign))
        .accounts({
            vote: messagePda,
        })
        .instruction();

    const { blockhash } = await provider.connection.getLatestBlockhash();

    const tx = new Transaction({
        feePayer: payerPubkey,
        recentBlockhash: blockhash,
    }).add(ix);

    const unsignedBase64 = tx.serialize({ requireAllSignatures: false }).toString("base64");

    const txPayerSigned = await SignTransaction(unsignedBase64);

    const txFullySigned = await provider.wallet.signTransaction(txPayerSigned);

    await provider.connection.sendRawTransaction(
        txFullySigned.serialize({ requireAllSignatures: true }),
        { skipPreflight: false }
    );
}