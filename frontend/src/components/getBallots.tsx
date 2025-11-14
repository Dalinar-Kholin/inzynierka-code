import {Button} from "@mui/material";
import {useState} from "react";
import getVotingPackage from "../api/getVotingPackage.ts";
import useBallot from "../context/ballot/useBallot.ts";

export default function GetBallots(){
    const [text, setText] = useState<string>("")
    const ballotCtx = useBallot()


    const getApiCall = async () => {
        const data = await getVotingPackage({sign: "chuj"})

        ballotCtx.setAckCode(data.ackCode)
        ballotCtx.setAuthSerial(data.authSerial)
        ballotCtx.setVoteSerial(data.voteSerial)
        ballotCtx.setVoteCodes(data.voteCodes)

        setText(JSON.stringify(data))
    }

    return(
        <>
            <p>{text}</p>
            <Button onClick={getApiCall} variant="contained">get ballot</Button>
        </>
    )
}