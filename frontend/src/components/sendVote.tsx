import {useState} from "react";
import {Button} from "@mui/material";
import {consts} from "../const.ts";
import getAuthCode from "../api/getAuthCode.ts";
import pingServerForAcceptVote from "../api/pingServerForAcceptVote.ts";
import getVoteCode from "../api/getVoteCode.ts";
import castVoteCode from "../api/castVote.ts";
import {useAnchor} from "../hooks/useAnchor.ts";
import getCastedVotes from "../api/getCastedVotes.ts";
import commitVote from "../api/commitVote.ts";

export default function SendVote(){
    const [authSerial, setAuthSerial] = useState<string>('');
    const [authCode, setAuthCode] = useState<string>('');
    const [voteSerial, setVoteSerial] = useState<string>('');
    const [bit, setBit] = useState<boolean>(false);
    const [casted, setCasted] = useState<string[]>([]);
    const [ackCode , setAckCode] = useState<string>('');
    const { getProgram, getProvider } = useAnchor();

    const getAuthCodeFunc = (async() => {
        const res =  await getAuthCode({ authSerial, bit })
        if (res.result === "")
            return

        consts.AUTH_CODE = res.result
        setAuthCode(res.result)
    })

    const getVoteCodeFunc = (async() => {
        consts.VOTE_CODES =  await getVoteCode({voteSerial: consts.VOTE_SERIAL})
        console.log(consts.VOTE_CODES)

    })

    const getAcceptedVote = (async() => {
        const program = getProgram()
        const all = await program.account.vote.all();

        console.log(all[0])
        setAckCode(new TextDecoder().decode(Uint8Array.from(all.filter(({ account }) => {
            return new TextDecoder().decode(Uint8Array.from(account.authCode)) === consts.AUTH_CODE
        })[0].account.ackCode)))
    })

    return (
        <>

            <Button onClick={()=> {
                setBit(!bit); /*obiviousTransfer use 1 or 2 authCode*/
            }}> set bit already := {bit ? "use first authCode" : "use second authCode"}</Button>
            <p>auth code _{authCode}_</p>
            <p>
                vote codes{consts.VOTE_CODES}
            </p>
            <p>ack code := {ackCode}</p>
            <p>{voteSerial}</p>
            <p>{authSerial}</p>
            <input onChange={e=> {setVoteSerial(e.target.value)}} value={voteSerial} />
            <p></p>
            <input onChange={e=> {setAuthSerial(e.target.value)}} value={authSerial} />
            <p></p>
            {consts.VOTE_CODES.map(code =>
                <p key={code}>
                    {<Button onClick={() => castVoteCode({voteCode: code, authCode: authCode, program: getProgram(), provider: getProvider()})}>{code.toUpperCase()}</Button>}
                </p>)}

            <Button onClick={()=>{
                setAuthSerial(consts.AUTH_SERIAL)
                setVoteSerial(consts.VOTE_SERIAL)
            }}>set stored AuthSerial and VoteSerial</Button>
            <Button onClick={async () => await getVoteCodeFunc()}>get vote Code</Button>
            <Button onClick={()=>{
                consts.VOTE_SERIAL = voteSerial
            }}>set vote serial</Button>
            <Button onClick={getAuthCodeFunc}>get Auth Code</Button>

            <Button onClick={async ()=>{
                setCasted(await getCastedVotes({program: getProgram()}));
            }}> get casted</Button>
            <Button onClick={getAcceptedVote}>look at accepted</Button>
            <Button onClick={async () => await pingServerForAcceptVote({sign: "", authCode: consts.AUTH_CODE, voteSerial: consts.VOTE_SERIAL})}>AcceptVote</Button>
            <Button onClick={async () => commitVote({sign: "A".repeat(64), authCode: consts.AUTH_CODE, program: getProgram(), provider: getProvider()})}>commit Vote</Button>
            {casted.map((casted) => <p key={casted}>{casted}</p>)}
        </>
    )
}