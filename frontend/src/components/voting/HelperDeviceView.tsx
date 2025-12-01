import BallotDataPrint from "../BallotDataPrint.tsx";
import GetVoteStatus from "../getVoteStatus.tsx";
import {Alert} from "@mui/material"
import {useHelperDevice} from "../../hooks/useHelperDevice.ts";
import DownloadXMLFile from "../downloadXMLFile.tsx";
import {UploadSignedVoteRequest} from "../UploadSignedVote.tsx";


export default function HelperDeviceView(){
    const {
        authSerial,
        voteSerial,
        voteCodes,
        successMessage,
        errorMessage,
        content,

        GetBallot,
        showError,
        showSuccess
    } = useHelperDevice()


    return(
        <>
            <BallotDataPrint authSerial={authSerial} voteSerial={voteSerial} authCode={""}/>
            {voteCodes?.map(c => <p key={c}>{c}</p>)}
            <p></p>
            <DownloadXMLFile content={content} name={"Download XML Vote Request"}></DownloadXMLFile>
            <UploadSignedVoteRequest GetBallot={GetBallot}></UploadSignedVoteRequest>
            <GetVoteStatus setErrorMessage={showError} setSuccessMessage={showSuccess}/>
            {successMessage !== null ?  <Alert severity="success">{successMessage}</Alert> : <></>}
            {errorMessage !== null ? <Alert severity="error">{errorMessage}</Alert> : <></>}
        </>
    )
}