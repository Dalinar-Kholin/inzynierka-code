import {Button} from "@mui/material";
import {useState} from "react";
import getVotingPackage from "../api/getVotingPackage.ts";
import {consts} from "../const.ts";
import {useNavigate} from "react-router-dom";

export default function GetBallots(){
    const [text, setText] = useState<string>("")
    const navigate = useNavigate();

    const getApiCall = async () => {
        const data = await getVotingPackage({sign: "chuj"})
        consts.ACK_CODE = data.ackCode
        consts.AUTH_SERIAL = data.authSerial
        consts.VOTE_SERIAL = data.voteSerial
        consts.VOTE_CODES = data.voteCodes

        setText(JSON.stringify(data))
    }

    const getAuthCode = async () => {

    }

    return(
        <>
            <p>{text}</p>
            <Button onClick={getApiCall} variant="contained">get ballot</Button>
            <Button onClick={()=> {navigate('/sendVote')}}>send vote</Button>
            <Button onClick={getAuthCode} variant="contained">get authCode</Button>
        </>
    )
}