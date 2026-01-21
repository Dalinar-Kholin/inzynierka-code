import GetVoteStatus from "../getVoteStatus.tsx";
import {Alert} from "@mui/material"
import {useHelperDevice} from "../../hooks/useHelperDevice.ts";
import DownloadXMLFile from "../downloadXMLFile.tsx";
import {UploadContentToState, UploadSignedVoteRequest} from "../UploadSignedVote.tsx";
import VotingPackCard from "../showData.tsx";

export default function HelperDeviceView() {
    const {
        votePack,
        successMessage,
        errorMessage,
        content,

        GetBallot,
        showError,
        setPublicKey,
        showSuccess
    } = useHelperDevice()

    return (
        <>
            <p></p>
            <DownloadXMLFile content={content} filename={"voteRequest.xml"}
                             name={"Download XML Vote Request"}></DownloadXMLFile>
            <UploadSignedVoteRequest GetBallot={GetBallot}></UploadSignedVoteRequest>
            <GetVoteStatus setErrorMessage={showError} setSuccessMessage={showSuccess}/>
            <UploadContentToState setContent={setPublicKey} name={"set Public Key"}></UploadContentToState>
            {successMessage !== null ? <Alert severity="success">{successMessage}</Alert> : <></>}
            {errorMessage !== null ? <Alert severity="error">{errorMessage}</Alert> : <></>}
            {votePack && <VotingPackCard pack={votePack} title="Pack #1" />}
        </>
    )
}