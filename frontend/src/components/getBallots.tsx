import {Button} from "@mui/material";
import {useState} from "react";
import getVotingPackage from "../api/getVotingPackage.ts";
import useBallot from "../context/ballot/useBallot.ts";

interface IGetBallots {
    setErrorMessage: (message: string) => void;
    setSuccessMessage: (message: string) => void;
}

export function GetBallots({setErrorMessage}: IGetBallots) {
    const [text, setText] = useState<string>("")
    const ballotCtx = useBallot()


    const getApiCall = async () => {
        const data = await getVotingPackage({sign: "chuj"}).catch(e => setErrorMessage(e.message))
        if (data === undefined) {
            return
        }

        ballotCtx.setAckCode(data.ackCode)
        ballotCtx.setAuthSerial(data.authSerial)
        ballotCtx.setVoteSerial(data.voteSerial)
        ballotCtx.setVoteCodes(data.voteCodes)

        setText(JSON.stringify(data))
    }

    return (
        <>
            <p>{text}</p>
            <Button onClick={getApiCall} variant="contained">get ballot</Button>
        </>
    )
}