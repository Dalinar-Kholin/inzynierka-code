import {PublicKey, Transaction} from "@solana/web3.js";
import type {AnchorProvider, Program} from "@coral-xyz/anchor";
import * as anchor from "@coral-xyz/anchor";
import {type ISignTransaction, type ISignTransactionResponse} from "./signTransaction.ts";
import type {Counter} from "../counter.ts";
import useSignTransaction from "./signTransaction.ts";

interface ICastVoteCode {
    voteCode: string;
    authCode: string;
    program: Program<Counter>;
    provider: AnchorProvider;
    setNewAccessCode: (newAccessCode: string) => void;
}

export default function useCastVoteCode(){
    const {sign} = useSignTransaction()

    async function castVote({voteCode, authCode, program, provider, setNewAccessCode} : ICastVoteCode): Promise<string>{
        return await castVoteCode({ voteCode, authCode, program, provider, setNewAccessCode, sign})
    }

    return {
        castVote: castVote,
    }
}

interface ICastVoteCodeHelper {
    voteCode: string;
    authCode: string;
    program: Program<Counter>;
    provider: AnchorProvider;
    setNewAccessCode: (newAccessCode: string) => void;
    sign: ({transaction, accessCode, authCode}: ISignTransaction) => Promise<ISignTransactionResponse>
}


async function castVoteCode({voteCode, authCode, program, provider, setNewAccessCode, sign} : ICastVoteCodeHelper){
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
