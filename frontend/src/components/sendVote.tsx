import {useState} from "react";
import {Button} from "@mui/material";
import {consts} from "../const.ts";
import {useWallet} from "@solana/wallet-adapter-react";
import getAuthCode from "../api/getAuthCode.ts";
import type {Program} from "@coral-xyz/anchor";
import type {Counter} from "../counter.ts";
import { PublicKey, SystemProgram, Transaction } from "@solana/web3.js";

export default function SendVote({ program }: { program: Program<Counter> | null }){
    const [authSerial, setAuthSerial] = useState<string>('');
    const [authCode, setAuthCode] = useState<string>('');
    const [voteSerial, setVoteSerial] = useState<string>('');
    const [bit, setBit] = useState<boolean>(false);

    const { publicKey} = useWallet();

    const getAuthCodeFunc = (async() => {

        const res =  await getAuthCode({ authSerial, bit })
        if (res.result === "")
            return

        consts.AUTH_CODE = res.result
        setAuthCode(res.result)
    })

    const sendVote = ( async (voteCode: string, authCode: string) => {
        // public vote on BB
        const enc = new TextEncoder();
        const authU8 = enc.encode(authCode); // Uint8Array length 64
        const voteU8 = enc.encode(voteCode); // Uint8Array length 32

        const authCodeNumbered: number[] = Array.from(authU8);
        const voteCodeNumbered: number[] = Array.from(voteU8);


        const payerPubkey = new PublicKey("6zuVDoqf3KZmAWgDaqQK1K7XkmXnDyyPpCJneAYuyky1");

        const [messagePda] = PublicKey.findProgramAddressSync(
            [Buffer.from("castVote"), voteCode.toArrayLike(Buffer, "le", 8), authCode.toArrayLike(Buffer, "le", 8)],
            program?.programId as PublicKey
        );


        // todo: dogarnąć kont 
        const t = await program?.methods.castVote(
            authCodeNumbered as never, voteCodeNumbered as never
        ).accounts({
            user: publicKey?.toBase58(),
            payer: payerPubkey,

        })

        const ix = await program.methods
            .create(index, message)
            .accounts({
                user: userPubkey,
                payer: payerPubkey,
                messageAccount: messagePda,
                systemProgram: SystemProgram.programId,
            })
            .instruction();



        // ping server to accept vote
    })

    return (
        <>

            <Button onClick={()=> {
                setBit(!bit); /*obiviousTransfer use 1 or 2 authCode*/
            }}> set bit already := {bit ? "use first authCode" : "use second authCode"}</Button>
            {publicKey?.toBase58()}
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
        </>
    )
}