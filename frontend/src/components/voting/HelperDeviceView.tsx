import BallotDataPrint from "../BallotDataPrint.tsx";
import GetVoteStatus from "../getVoteStatus.tsx";
import {Alert} from "@mui/material"
import {useHelperDevice} from "../../hooks/useHelperDevice.ts";
import DownloadXMLFile from "../downloadXMLFile.tsx";
import {UploadContentToState, UploadSignedVoteRequest} from "../UploadSignedVote.tsx";
import {useAnchor} from "../../hooks/useAnchor.ts";
import {useEffect} from "react";
import {Box} from "@mui/material";

export default function HelperDeviceView(){
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
        <Box sx={{
            display: "grid",
            gridTemplateColumns: "repeat(6, minmax(0, 1fr))",
            gap: 2,
        }}>
            { votePack ? <ShowVoteStatus voteSerial={votePack.votes[0].voteSerial} authSerial={votePack.authSerial} voteCodes={votePack.votes[0].voteCodes} /> : <></>}
            { votePack ? <ShowVoteStatus voteSerial={votePack.votes[1].voteSerial} authSerial={votePack.authSerial} voteCodes={votePack.votes[1].voteCodes} /> : <></>}
        </Box>
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

interface ShowVoteStatus {
    authSerial: string
    voteSerial: string
    voteCodes: string[]
}

function ShowVoteStatus({authSerial, voteSerial, voteCodes}: ShowVoteStatus){
    return <>
        <BallotDataPrint authSerial={authSerial} voteSerial={voteSerial} authCode={""}/>
        {voteCodes?.map(c => <p key={c}>{c}</p>)}
    </>
}