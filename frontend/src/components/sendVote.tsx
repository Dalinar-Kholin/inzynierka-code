import {useState} from "react";
import {Button} from "@mui/material";
import {consts} from "../const.ts";
import getAuthCode from "../api/getAuthCode.ts";
import { PublicKey, Transaction } from "@solana/web3.js";
import * as anchor from "@coral-xyz/anchor";
import {useAnchor} from "../hooks/useAnchor.ts";
import SignTransaction from "../api/signTransaction.ts";
import pingServerForAcceptVote from "../api/pingServerForAcceptVote.ts";

export default function SendVote(){
    const [authSerial, setAuthSerial] = useState<string>('');
    const [authCode, setAuthCode] = useState<string>('');
    const [voteSerial, setVoteSerial] = useState<string>('');
    const [bit, setBit] = useState<boolean>(false);
    const { getProgram, getProvider } = useAnchor();
    const [casted, setCasted] = useState<string[]>([]);

    const getAuthCodeFunc = (async() => {

        const res =  await getAuthCode({ authSerial, bit })
        if (res.result === "")
            return

        consts.AUTH_CODE = res.result
        setAuthCode(res.result)
    })

    const getCastedVotes = (async() => {
        const program = getProgram()
        const all = await program.account.castVote.all();

        const rows = all.map(({ account }) => {
            const voteUtf8 = new TextDecoder().decode(
                Uint8Array.from(account.voteCode)    // [u8;3] -> string like "ala"
            );
            return `${voteUtf8} — (${account.authCode})`;
        });

// functional update avoids stale `casted`
        setCasted(rows);
    })

    const sendVote = ( async (voteCode: string, authCode: string) => {
        // public vote on BB
        const program = getProgram();
        const provider = getProvider();

        const enc = new TextEncoder();
        const authU8 = enc.encode(authCode);
        const voteU8 = enc.encode(voteCode);

        if (authU8.length !== 64) throw new Error(`authCode must be 64 bytes, got ${authU8.length}`);
        if (voteU8.length !== 3) throw new Error(`voteCode must be 3 bytes, got ${voteU8.length}`);

        const payerPubkey = new PublicKey("6zuVDoqf3KZmAWgDaqQK1K7XkmXnDyyPpCJneAYuyky1");

        const userPubkey = provider.wallet.publicKey;

        const messageSeedPrefix = anchor.utils.bytes.utf8.encode("castVote");



        const [messagePda] = PublicKey.findProgramAddressSync(
            [messageSeedPrefix, Buffer.from(voteU8).subarray(0, 3), Buffer.from(authU8.slice(0, 32))],
            program.programId
        );

        const ix = await program.methods
            .castVote(Buffer.from(voteU8), Buffer.from(authU8))
            .accounts({
                user: userPubkey,                  // Pass PublicKey, not base58 string
                payer: payerPubkey,
                cast: messagePda,
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

        const sig = await provider.connection.sendRawTransaction(
            txFullySigned.serialize({ requireAllSignatures: true }),
            { skipPreflight: false }
        );

        console.log("Signature:", sig);

        // ping server to accept vote
    })

    return (
        <>

            <Button onClick={()=> {
                setBit(!bit); /*obiviousTransfer use 1 or 2 authCode*/
            }}> set bit already := {bit ? "use first authCode" : "use second authCode"}</Button>
            <p>
                auth code _{authCode}_
            </p>

            <p>{voteSerial}</p>
            <p>{authSerial}</p>
            <input onChange={e=> {setVoteSerial(e.target.value)}} value={voteSerial} />
            <p></p>
            <input onChange={e=> {setAuthSerial(e.target.value)}} value={authSerial} />
            <p></p>
            {consts.VOTE_CODES.map(code =>
                <p key={code}>
                    {<Button onClick={() => sendVote(code, authCode)}>{code.toUpperCase()}</Button>}
                </p>)}

            <Button onClick={()=>{
                setAuthSerial(consts.AUTH_SERIAL)
                setVoteSerial(consts.VOTE_SERIAL)
            }}>set stored AuthSerial and VoteSerial</Button>
            <Button onClick={getAuthCodeFunc}>get Auth Code</Button>
            <Button onClick={getCastedVotes}> get casted</Button>
            <Button onClick={async () => await pingServerForAcceptVote({sign: "", authSerial: ""})}>AcceptVote</Button>
            {casted.map((casted) => <p>{casted}</p>)}
        </>
    )
}