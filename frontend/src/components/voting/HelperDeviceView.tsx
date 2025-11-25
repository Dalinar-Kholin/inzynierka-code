import BallotDataPrint from "../BallotDataPrint.tsx";
import GetVoteStatus from "../getVoteStatus.tsx";
import {Alert, Button} from "@mui/material"
import {useHelperDevice} from "../../hooks/useHelperDevice.ts";

export default function HelperDeviceView(){
    const {
        authSerial,
        voteSerial,
        voteCodes,
        successMessage,
        errorMessage,

        GetBallot,
        showError,
        showSuccess
    } = useHelperDevice()


    return(
        <>
            <BallotDataPrint authSerial={authSerial} voteSerial={voteSerial} authCode={undefined}/>
            {voteCodes?.map(c => <p>{c}</p>)}
            <p></p>

            <Button onClick={GetBallot} variant="contained">get ballot</Button>
            <GetVoteStatus setErrorMessage={showError} setSuccessMessage={showSuccess}/>
            {successMessage !== null ?  <Alert severity="success">{successMessage}</Alert> : <></>}
            {errorMessage !== null ? <Alert severity="error">{errorMessage}</Alert> : <></>}

        </>
    )
}