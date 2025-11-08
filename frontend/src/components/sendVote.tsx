import {useState} from "react";
import {Button} from "@mui/material";
import {consts} from "../const.ts";
import {useWallet} from "@solana/wallet-adapter-react";
import getAuthCode from "../api/getAuthCode.ts";

export default function SendVote(/*{ program }: { program: Program<Counter> | null }*/){
    const [authSerial, setAuthSerial] = useState<string>('');
    const [authCode, setAuthCode] = useState<string>('');
    const [voteSerial, setVoteSerial] = useState<string>('');
    const [bit, setBit] = useState<boolean>(false);

    const { publicKey} = useWallet();

    const getAuthCodeFunc = (async() => {

        const res =  await getAuthCode({ authSerial, bit })
        setAuthCode(res.result)
    })


    return (
        <>

            <Button onClick={()=> {
                setBit(!bit);
            }}> set bit already := {`${bit}`}</Button>
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
                    {/*<Button onClick={() => makeVote(code)}>{code.toUpperCase()}</Button>*/}
                </p>)}

            <Button onClick={()=>{
                setAuthSerial(consts.AUTH_SERIAL)
                setVoteSerial(consts.VOTE_SERIAL)
            }}>set stored AuthSerial and VoteSerial</Button>
            <Button onClick={getAuthCodeFunc}>get Auth Code</Button>
        </>
    )
}