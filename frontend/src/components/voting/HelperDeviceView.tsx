import BallotDataPrint from "../BallotDataPrint.tsx";
import GetVoteStatus from "../getVoteStatus.tsx";
import {Alert} from "@mui/material"
import {useHelperDevice} from "../../hooks/useHelperDevice.ts";
import DownloadXMLFile from "../downloadXMLFile.tsx";
import {UploadContentToState, UploadSignedVoteRequest} from "../UploadSignedVote.tsx";


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
        setPublicKey,
        showSuccess
    } = useHelperDevice()


    return(
        <>
            <BallotDataPrint authSerial={authSerial} voteSerial={voteSerial} authCode={""}/>
            {voteCodes?.map(c => <p key={c}>{c}</p>)}
            <p></p>
            <DownloadXMLFile content={content} filename={"voteRequest"} name={"Download XML Vote Request"}></DownloadXMLFile>
            <UploadSignedVoteRequest GetBallot={GetBallot}></UploadSignedVoteRequest>
            <GetVoteStatus setErrorMessage={showError} setSuccessMessage={showSuccess}/>
            <UploadContentToState setContent={setPublicKey} name={"set Public Key"}></UploadContentToState>
            {successMessage !== null ?  <Alert severity="success">{successMessage}</Alert> : <></>}
            {errorMessage !== null ? <Alert severity="error">{errorMessage}</Alert> : <></>}
        </>
    )
}