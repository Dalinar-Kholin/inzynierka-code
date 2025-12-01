import {useAnchor} from "../hooks/useAnchor.ts";
import {useEffect, useRef, useState} from "react";
import type {IdlAccounts} from "@coral-xyz/anchor";
import type {Counter} from "../counter.ts";

import {stringify} from "uuid";
import {Button} from "@mui/material";

type VoteAccountType = IdlAccounts<Counter>["vote"];
type VotingStage = VoteAccountType["stage"];

interface accountData {
    VotingStage: VotingStage;
    AuthSerial: string;
    VoteSerial: string;
    VoteCode: string;
    AuthCode: string;
    ServerSign: number[];
    VoterSign: string;
}

type StageName = keyof VotingStage; // "empty" | "casted" | "accepted" | "committed"

function getStageName(stage: VotingStage): StageName {
    const [key] = Object.keys(stage) as StageName[];
    return key;
}

interface IGetVoteStatus{
    setErrorMessage: (message: string) => void;
    setSuccessMessage: (message: string) => void;
}

export default function GetVoteStatus({setErrorMessage, setSuccessMessage}: IGetVoteStatus) {
    const {getProgram} = useAnchor()
    const [authSerial, setAuthSerial] = useState<string>("")
    const [accountData, setAccountData] = useState<accountData[]>([])
    const key = useRef<string>("")

    useEffect(() => { // load Key
        const fetch = async () => {
            key.current = (new TextDecoder("utf-8")).decode(new Uint8Array((await getProgram().account.signKey.all())[0].account.key))
        }
        fetch().then();
    }, []);

    useEffect(() => {
        const fetchData = async () => {
            const data: accountData[] = []
            const res = await getProgram().account.vote.all()
            const decoder = new TextDecoder("utf-8");
            res.forEach(r => {
                const newItem: accountData = {
                    VotingStage: r.account.stage,
                    AuthSerial: stringify(new Uint8Array(r.account.authSerial)),
                    VoteSerial: stringify(new Uint8Array(r.account.voteSerial)),
                    VoteCode: decoder.decode(new Uint8Array(r.account.voteCode)),
                    AuthCode: decoder.decode(new Uint8Array(r.account.authCode)),
                    ServerSign: r.account.serverSign,
                    VoterSign: decoder.decode(new Uint8Array(r.account.voterSign))
                }
                data.push(newItem)
            })
            setAccountData(data)
        };
        fetchData().then(r => console.log(r));
    }, [authSerial])


    return <>
        <p>
            <input onChange={e => {
                setAuthSerial(e.target.value)
            }} value={authSerial}/>
        </p>
        <div>
            {authSerial.length > 5 ? accountData.filter(ad => ad.AuthSerial.substring(0, authSerial.length) === authSerial).map(ad => (
                <div key={ad.AuthSerial}>
                    <p>voting stage := {getStageName(ad.VotingStage)}</p>
                    <p>auth Code := {ad.AuthCode}</p>
                    <p>vote code := {ad.VoteCode}</p>
                    <p>vote serial := {ad.VoteSerial}</p>
                    <p>auth serial := {ad.AuthSerial}</p>
                    <p>
                        <Button onClick={async () => {
                            const signedObject: BackendLookPack = {
                                Stage: 1, // server już zcommtował więc na BB jest voting stage 2 albo nawet dalej więc trzeba zhardcodować
                                VoteCode: stringToByteArray(ad.VoteCode),
                                AuthCode: stringToByteArray(ad.AuthCode),
                            };
                            const res = await verifyEd25519(key.current, JSON.stringify(signedObject), Buffer.from(ad.ServerSign).toString('base64'), "base64")
                            res ? setSuccessMessage("good signature") : setErrorMessage("bad signature")
                        }}>Verify Server Sign</Button>
                    </p>

                </div>
            )) : <></>}
        </div>
    </>
}


interface BackendLookPack {
    Stage: number;
    VoteCode: number[];
    AuthCode: number[];
}

function pemToArrayBuffer(pem: string): ArrayBuffer {
    console.log(pem);
    const b64 = pem.replace("-----BEGIN PUBLIC KEY-----", "").replace("-----END PUBLIC KEY-----", "")

    const binary = atob(b64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }
    return bytes.buffer;
}

function base64ToUint8(b64: string): Uint8Array {
    const bin = atob(b64);
    const out = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) out[i] = bin.charCodeAt(i);
    return out;
}

// If signature comes as hex:
function hexToUint8(hex: string): Uint8Array {
    const clean = hex.startsWith("0x") ? hex.slice(2) : hex;
    const out = new Uint8Array(clean.length / 2);
    for (let i = 0; i < out.length; i++) {
        out[i] = parseInt(clean.slice(i * 2, i * 2 + 2), 16);
    }
    return out;
}

// ---- main verify ----
export async function verifyEd25519(
    publicKeyPem: string,
    message: string | Uint8Array,
    signature: string,                 // base64 OR hex
    signatureEncoding: "base64" | "hex" = "base64"
): Promise<boolean> {
    const spkiDer = pemToArrayBuffer(publicKeyPem);

    const publicKey = await crypto.subtle.importKey(
        "spki",
        spkiDer,
        {name: "Ed25519"},
        false,
        ["verify"]
    );

    const msgBytes =
        typeof message === "string"
            ? new TextEncoder().encode(message)
            : message;

    const sigBytes =
        signatureEncoding === "base64"
            ? base64ToUint8(signature)
            : hexToUint8(signature);

    console.log(sigBytes);
    // @ts-ignore
    return crypto.subtle.verify({name: "Ed25519"}, publicKey, sigBytes, msgBytes);
}

function stringToByteArray(str: string): number[] {
    const encoder = new TextEncoder();       // UTF-8
    return Array.from(encoder.encode(str));  // Uint8Array → number[]
}
