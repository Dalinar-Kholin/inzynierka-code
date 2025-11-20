import {PublicKey, Transaction} from "@solana/web3.js";
import * as anchor from "@coral-xyz/anchor";
import SignTransaction from "./signTransaction.ts";
import type {Counter} from "../counter.ts";
import type {AnchorProvider, Program} from "@coral-xyz/anchor";

interface ICastVoteCode {
    voteCode: string;
    authCode: string;
    program: Program<Counter>;
    provider: AnchorProvider;
}

export default async function castVoteCode({voteCode, authCode, program, provider} : ICastVoteCode){

    const enc = new TextEncoder();
    const authU8 = enc.encode(authCode);
    const voteU8 = enc.encode(voteCode);

    if (authU8.length !== 64) throw new Error(`authCode must be 64 bytes, got ${authU8.length}`);
    if (voteU8.length !== 3) throw new Error(`voteCode must be 3 bytes, got ${voteU8.length}`);

    const payerPubkey = new PublicKey("6zuVDoqf3KZmAWgDaqQK1K7XkmXnDyyPpCJneAYuyky1");


    const messageSeedPrefix = anchor.utils.bytes.utf8.encode("commitVote");



    const [messagePda] = PublicKey.findProgramAddressSync(
        [messageSeedPrefix, Buffer.from(authU8.slice(0, 32)), Buffer.from(authU8.slice(32, 64))],
        program.programId
    );

    const ix = await program.methods
        .castVote(Buffer.from(authU8), Buffer.from(voteU8))
        .accounts({
            payer: payerPubkey,
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

    await provider.connection.sendRawTransaction(
        txPayerSigned.serialize({ requireAllSignatures: true }),
        { skipPreflight: false }
    );
}
