import {useAnchor} from "../hooks/useAnchor.ts";
import {useEffect, useState} from "react";
import type {IdlAccounts} from "@coral-xyz/anchor";
import type {Counter} from "../counter.ts";

import { stringify } from "uuid";

type VoteAccountType = IdlAccounts<Counter>["vote"];
type VotingStage = VoteAccountType["stage"];

interface accountData {
    votingStage: VotingStage;
    authSerial: string;
    voteSerial: string;
    voteCode: string;
    authCode: string;
    serverSign: string;
    voterSign: string;
}

type StageName = keyof VotingStage; // "empty" | "casted" | "accepted" | "committed"

function getStageName(stage: VotingStage): StageName {
    const [key] = Object.keys(stage) as StageName[];
    return key;
}

export default function GetVoteStatus() {
    const {getProgram} = useAnchor()
    const [authSerial, setAuthSerial] = useState<string>("")
    const [accountData, setAccountData] = useState<accountData[]>([])

    useEffect(() => {
        const fetchData = async () => {
            const data: accountData[] = []
            const res = await getProgram().account.vote.all()
            const decoder = new TextDecoder("utf-8");
            res.forEach(r => {
                const newItem : accountData = {
                    votingStage: r.account.stage,
                    authSerial: stringify(new Uint8Array(r.account.authSerial)),
                    voteSerial: stringify(new Uint8Array(r.account.voteSerial)),
                    voteCode: decoder.decode(new Uint8Array(r.account.voteCode)),
                    authCode: decoder.decode(new Uint8Array(r.account.authCode)),
                    serverSign: decoder.decode(new Uint8Array(r.account.serverSign)),
                    voterSign: decoder.decode(new Uint8Array(r.account.voterSign))
                }
                console.log(newItem.authSerial)
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
        <p>
            {authSerial.length <= 5 ?  <></> : <></>}
        </p>
        <p>
            {accountData.filter(ad => ad.authSerial.substring(0, authSerial.length) === authSerial).map(ad => (
                <p key={ad.authSerial}>
                    <p>voting stage := {getStageName(ad.votingStage)}</p>
                    <p>auth Code := {ad.authCode}</p>
                    <p>vote code := {ad.voteCode}</p>
                    <p>vote serial := {ad.voteSerial}</p>
                    <p>auth serial := {ad.authSerial}</p>
                    ---------------------------------------
                    {/*{ad.serverSign}
                    {ad.voterSign}*/}
                </p>
            ))}
        </p>
    </>
}