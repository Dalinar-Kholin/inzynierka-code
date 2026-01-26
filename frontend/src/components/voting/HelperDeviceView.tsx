import GetVoteStatus from "../getVoteStatus.tsx";
import { Alert, Stack } from "@mui/material";
import { useHelperDevice } from "../../hooks/useHelperDevice.ts";
import DownloadXMLFile from "../downloadXMLFile.tsx";
import { UploadContentToState, UploadSignedVoteRequest } from "../UploadSignedVote.tsx";
import {VotingPackCard} from "../showData.tsx";

export default function HelperDeviceView() {
    const {
        votePack,
        successMessage,
        errorMessage,
        content,

        GetBallot,
        showError,
        setPublicKey,
        showSuccess,
    } = useHelperDevice();

    return (
        <Stack spacing={2}>
            {successMessage && (<Alert severity="success">{successMessage}</Alert>)}
            {errorMessage && (<Alert severity="error">{errorMessage}</Alert>)}
            <DownloadXMLFile
                content={content}
                filename="voteRequest.xml"
                name="Download XML Vote Request"
            />

            <UploadSignedVoteRequest GetBallot={GetBallot} />

            <GetVoteStatus
                setErrorMessage={showError}
                setSuccessMessage={showSuccess}
            />

            <UploadContentToState
                setContent={setPublicKey}
                name="Set Public Key"
            />

            {votePack && (
                <VotingPackCard pack={votePack} title="Pack #1" />
            )}
        </Stack>
    );
}
