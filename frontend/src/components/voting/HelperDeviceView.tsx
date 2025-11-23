import {GetBallots} from "../getBallots.tsx";
import BallotDataPirnt from "../BallotDataPirnt.tsx";
import GetVoteStatus from "../getVoteStatus.tsx";
import UploadSignedDocument from "../UploadSignedDocument.tsx";
import {useState} from "react";
import {Alert} from "@mui/material"



export default function HelperDeviceView(){
    const [successMessage, setSuccessMessage] = useState<string>("")
    const [errorMessage, setErrorMessage] = useState<string>("")

    const setErrorWrapper = (m :string) => {
        setErrorMessage(m)
        setSuccessMessage("")
    }

    const setSuccessWrapper = (m :string) => {
        setErrorMessage("")
        setSuccessMessage(m)
    }

    return(
        <>
            <BallotDataPirnt />
            <p></p>
            <GetBallots setErrorMessage={setErrorWrapper} setSuccessMessage={setSuccessWrapper}/>
            <GetVoteStatus setErrorMessage={setErrorWrapper} setSuccessMessage={setSuccessWrapper}/>
            <UploadSignedDocument />
            {successMessage !== "" ? <Alert severity="success">{successMessage}</Alert> : <></>}
            {errorMessage !== "" ? <Alert severity="error">{errorMessage}</Alert> : <></>}

        </>
    )
}