import {useState} from "react";
import {Button} from "@mui/material";
import getAuthCode from "../api/getAuthCode.ts";
import pingServerForAcceptVote from "../api/pingServerForAcceptVote.ts";
import getVoteCode from "../api/getVoteCode.ts";
import castVoteCode from "../api/castVote.ts";
import {useAnchor} from "../hooks/useAnchor.ts";
import getCastedVotes from "../api/getCastedVotes.ts";
import commitVote from "../api/commitVote.ts";
import useBallot from "../context/ballot/useBallot.ts";

export default function SendVote(){
    const [bit, setBit] = useState<boolean>(false);
    const [casted, setCasted] = useState<string[]>([]);
    const { getProgram, getProvider } = useAnchor();
    const ballotCtx = useBallot()


    const getAuthCodeFunc = (async() => {
        const res =  await getAuthCode({ authSerial: ballotCtx.ballot.AUTH_SERIAL, bit })
        if (res.result === "")
            return
        console.error(res.result)
        ballotCtx.setAuthCode(res.result)
    })

    const getVoteCodeFunc = (async() => {
        ballotCtx.setVoteCodes(
            await getVoteCode({voteSerial: ballotCtx.ballot.VOTE_SERIAL})
        )
    })

    const getAcceptedVote = (async() => {
        const program = getProgram()
        const all = await program.account.vote.all();

        console.log(all[0])
        ballotCtx.setAckCode(new TextDecoder().decode(Uint8Array.from(all.filter(({ account }) => {
            return new TextDecoder().decode(Uint8Array.from(account.authCode)) === ballotCtx.ballot.AUTH_CODE
        })[0].account.ackCode)))
    })

    return (
        <>
            <Button onClick={()=> {
                setBit(!bit); /*obiviousTransfer use 1 or 2 authCode*/
            }}> set bit already := {bit ? "use first authCode" : "use second authCode"}</Button>
            <p>auth code _{ballotCtx.ballot.AUTH_CODE}_</p>
            <p>
                vote codes{ballotCtx.ballot.VOTE_CODES}
            </p>

            <p>ack code := {ballotCtx.ballot.ACK_CODE}</p>
            <p>{ballotCtx.ballot.VOTE_SERIAL}</p>
            <p>{ballotCtx.ballot.AUTH_SERIAL}</p>
            <input onChange={e=> {ballotCtx.setVoteSerial(e.target.value)}} value={ballotCtx.ballot.VOTE_SERIAL} />
            <p></p>
            <input onChange={e=> {ballotCtx.setAuthSerial(e.target.value)}} value={ballotCtx.ballot.AUTH_SERIAL} />
            <p></p>
            {ballotCtx.ballot.VOTE_CODES.map(code =>
                <p key={code}>
                    {<Button onClick={() => castVoteCode({voteCode: code, authCode: ballotCtx.ballot.AUTH_CODE, program: getProgram(), provider: getProvider()})}>{code.toUpperCase()}</Button>}
                </p>)}

            <Button onClick={async () => await getVoteCodeFunc()}>get vote Code</Button>
            <Button onClick={getAuthCodeFunc}>get Auth Code</Button>

            <Button onClick={async ()=>{
                setCasted(await getCastedVotes({program: getProgram()}));
            }}> get casted</Button>
            <Button onClick={getAcceptedVote}>look at accepted</Button>
            <Button onClick={async () => await pingServerForAcceptVote({sign: "", authCode: ballotCtx.ballot.AUTH_CODE, voteSerial: ballotCtx.ballot.VOTE_SERIAL})}>AcceptVote</Button>
            <Button onClick={async () => commitVote({sign: "A".repeat(64), authCode: ballotCtx.ballot.AUTH_CODE, program: getProgram(), provider: getProvider()})}>commit Vote</Button>
            {casted.map((casted) => <p key={casted}>{casted}</p>)}
        </>
    )
}