import {PublicKey, Transaction} from "@solana/web3.js";
import * as anchor from "@coral-xyz/anchor";
import {
    type useSignTransactionFnType
} from "./signTransaction.ts";
import type {Counter} from "../counter.ts";
import type {AnchorProvider, Program} from "@coral-xyz/anchor";
import pako from "pako";
import useSignTransaction from "./signTransaction.ts";

interface ICommitVote {
    signedDocument: string;
    authCode: string;
    program: Program<Counter>;
    provider: AnchorProvider;
    accessCode: string;
}


export default function useCommitVote(){
    const sign = useSignTransaction()

    async function commit({signedDocument, authCode, program, provider, accessCode} : ICommitVote){
        return await commitVote({ signedDocument, authCode, program, provider, accessCode, sign})
    }

    return {
        commit: commit,
    }
}


interface ICommitVoteHelper {
    signedDocument: string;
    authCode: string;
    program: Program<Counter>;
    provider: AnchorProvider;
    accessCode: string;
    sign: useSignTransactionFnType
}

async function commitVote({signedDocument, authCode, program, provider, accessCode, sign } : ICommitVoteHelper){
    const enc = new TextEncoder();
    const authU8 = enc.encode(authCode);

    const zippedDocument = compressGzipString(signedDocument);

    if (authU8.length !== 64) throw new Error(`authCode must be 64 bytes, got ${authU8.length}`);

    const payerPubkey = new PublicKey("6zuVDoqf3KZmAWgDaqQK1K7XkmXnDyyPpCJneAYuyky1");

    const messageSeedPrefix = anchor.utils.bytes.utf8.encode("commitVote");

    const [messagePda] = PublicKey.findProgramAddressSync(
        [messageSeedPrefix, Buffer.from(authU8.slice(0, 32)), Buffer.from(authU8.slice(32, 64))],
        program.programId
    );

    const { blockhash } = await provider.connection.getLatestBlockhash();


    const CHUNK_SIZE = 800; // bezpiecznie poniżej 1000 B
    let newAccessCode = accessCode;
    for (let offset = 0; offset < zippedDocument.length; offset += CHUNK_SIZE) {
        const tx = new Transaction({
            feePayer: payerPubkey,
            recentBlockhash: blockhash,
        })
        const chunk = zippedDocument.slice(offset, Math.min(offset + CHUNK_SIZE, zippedDocument.length));

        const ix = await program.methods
            .commitVote(Buffer.from(authU8), new anchor.BN(offset), Buffer.from(chunk))
            .accounts({ vote: messagePda })
            .instruction();
        tx.add(ix)
        const unsignedBase64 = tx.serialize({ requireAllSignatures: false }).toString("base64");
        const txPayerSigned = await sign({transaction: unsignedBase64, accessCode: newAccessCode, authCode: undefined});
        newAccessCode = txPayerSigned.newAccessCode;
        await provider.connection.sendRawTransaction(
            txPayerSigned.transaction.serialize({ requireAllSignatures: true }),
            { skipPreflight: false }
        );
    }
}

export function compressGzipString(input: string): Uint8Array {
    const data = new TextEncoder().encode(input);
    return pako.gzip(data, { level: 9 });
}

export function decompressGzipToString(input: Uint8Array): string {
    const decompressed = pako.ungzip(input);
    return new TextDecoder().decode(decompressed);
}