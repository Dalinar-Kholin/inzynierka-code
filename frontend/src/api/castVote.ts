import {PublicKey, Transaction} from "@solana/web3.js";
import type {AnchorProvider, Program} from "@coral-xyz/anchor";
import * as anchor from "@coral-xyz/anchor";
import {
    type useSignTransactionFnType
} from "./signTransaction.ts";
import type {Counter} from "../counter.ts";
import useSignTransaction from "./signTransaction.ts";

interface ICastVoteCode {
    voteSerial: string;
    lockCode: string;
    voteCode: string;
    authCode: string;
    program: Program<Counter>;
    provider: AnchorProvider;
    setNewAccessCode: (newAccessCode: string) => void;
}

export default function useCastVoteCode(){
    const sign = useSignTransaction()

    async function castVote({lockCode, voteSerial, voteCode, authCode, program, provider, setNewAccessCode} : ICastVoteCode): Promise<string>{
        return await castVoteCode({ lockCode, voteSerial, voteCode, authCode, program, provider, setNewAccessCode, sign})
    }

    return {
        castVote: castVote,
    }
}

interface ICastVoteCodeHelper {
    voteSerial: string;
    lockCode: string;
    voteCode: string;
    authCode: string;
    program: Program<Counter>;
    provider: AnchorProvider;
    setNewAccessCode: (newAccessCode: string) => void;
    sign: useSignTransactionFnType
}


async function castVoteCode({lockCode, voteSerial, voteCode, authCode, program, provider, setNewAccessCode, sign} : ICastVoteCodeHelper){
    const enc = new TextEncoder();
    const serialU8 = enc.encode(voteSerial)
    const authU8 = enc.encode(authCode);
    const voteU8 = enc.encode(voteCode);
    const lockU8 = enc.encode(lockCode);
    if (authU8.length !== 64) throw new Error(`authCode must be 64 bytes, got ${authU8.length}`);
    if (voteU8.length !== 10) throw new Error(`voteCode must be 3 bytes, got ${voteU8.length}`);

    const payerPubkey = new PublicKey("6zuVDoqf3KZmAWgDaqQK1K7XkmXnDyyPpCJneAYuyky1");

    const messageSeedPrefix = anchor.utils.bytes.utf8.encode("commitVote");

    console.log(authU8);

    const [messagePda] = PublicKey.findProgramAddressSync(
        [messageSeedPrefix, Buffer.from(authU8.slice(0, 32)), Buffer.from(authU8.slice(32, 64))],
        program.programId
    );

    const ix = await program.methods
        .castVote(Buffer.from(authU8), Buffer.from(serialU8), Buffer.from(voteU8), Buffer.from(lockU8))
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

    const txPayerSigned = await sign({transaction: unsignedBase64, authCode: authCode, accessCode: undefined });
    setNewAccessCode(txPayerSigned.newAccessCode)

    const unsignedMessage = tx.serializeMessage();
    const signedMessage = txPayerSigned.transaction.serializeMessage();

    if(!Buffer.from(unsignedMessage).equals(Buffer.from(signedMessage))){
        throw new Error("server try to change your vote");
    }
    
    return await provider.connection.sendRawTransaction(
        txPayerSigned.transaction.serialize({requireAllSignatures: true}),
        {skipPreflight: false}
    );
}