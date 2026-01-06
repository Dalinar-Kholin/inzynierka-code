import BallotDataPrint from "../BallotDataPrint.tsx";
import GetVoteStatus from "../getVoteStatus.tsx";
import {Alert} from "@mui/material"
import {useHelperDevice} from "../../hooks/useHelperDevice.ts";
import DownloadXMLFile from "../downloadXMLFile.tsx";
import {UploadContentToState, UploadSignedVoteRequest} from "../UploadSignedVote.tsx";
import {useAnchor} from "../../hooks/useAnchor.ts";
import {useEffect} from "react";


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

    const {getProgram} = useAnchor()

    useEffect(()=> {
        const fetch = async () => {
            const value = (await getProgram().account.singleCommitment.all())[0].account.toCommit

            console.log(Buffer.from(value).toString("hex"))
        }
        fetch().then()
    })

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