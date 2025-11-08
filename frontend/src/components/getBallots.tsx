import {Button} from "@mui/material";
import {useState} from "react";
import getVotingPackage from "../api/getVotingPackage.ts";
import {consts} from "../const.ts";

export default function GetBallots(){
    const [text, setText] = useState<string>("")

    const getApiCall = async () => {
        const data = await getVotingPackage({sign: "chuj"})
        consts.ACK_CODE = data.ackCode
        consts.AUTH_SERIAL = data.authSerial
        consts.VOTE_SERIAL = data.voteSerial
        consts.VOTE_CODES = data.voteCodes

        setText(JSON.stringify(data))
    }

    return(
        <>
            <p>{text}</p>
            <Button onClick={getApiCall} variant="contained">get ballot</Button>
        </>
    )
}