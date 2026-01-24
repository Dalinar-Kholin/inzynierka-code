import {Alert, Button} from "@mui/material";
import BallotDataPrint from "../BallotDataPrint.tsx";
import DownloadXMLFile from "../downloadXMLFile.tsx";
import UploadSignedVote from "../UploadSignedVote.tsx";
import ResponsiveDialog from "../obliviousTransferDialog.tsx";
import useVoting from "../../hooks/useVoting.ts";
import Vote, {serializeVoteToXML} from "../../helpers/XMLbuilder.ts";
import InputForm from "../InputForm.tsx";

export default function VotingDeviceView() {
    const {
        bit,
        authSerial,
        otPack,
        voteSerial,
        voteCodes,
        successMessage,
        errorMessage,
        selectedCode,
        voterSign,
        serverSign,
        accessCode,
        lockCode,
        commitment,
        PingForAccept,
        setVoteSerial,
        setAuthSerial,
        setVoterSign,
        setBit,
        CastVote,
        GetVoteCodes,
        GetAuthCodes,
        setLockCode,
        GetAcceptedBallot
    } = useVoting()

    return (<>


            <InputForm name={"lockCode"} fn={setLockCode} value={lockCode} />
            <InputForm name={"authSerial"} fn={setAuthSerial} value={authSerial} />
            <InputForm name={"voteSerial"} fn={setVoteSerial} value={voteSerial} />

            <p></p>
            {voteCodes?.map(code =>
                <p key={code}>
                    {<Button onClick={() =>
                        CastVote(code)
                    }>{code}</Button>}
                </p>
            )}

            <Button onClick={async () => {
                await GetVoteCodes()
            }}>get vote codes</Button>
            <div>
                <p>Auth code Oblivious Transfer</p>
                <p>
                    <ResponsiveDialog setUseFirstAuthCode={(v) => setBit(v)}/>
                </p>
                <p>
                    {bit === undefined
                        ? <Button variant="outlined" color="error">get AuthCode (firstly choose which code to choose)</Button>
                        : <Button onClick={GetAuthCodes}>get AuthCode</Button>}
                </p>
            </div>
            <p>
                ping server to Accept Vote
                <Button onClick={async () => {
                    await PingForAccept()
                }}>AcceptVote</Button>
            </p>
            <p>
                <Button onClick={GetAcceptedBallot}>
                    get AcceptedBallot
                </Button>
            </p>

            <BallotDataPrint
                voteSerial={voteSerial} authSerial={authSerial} authCode={otPack?.authCode || ""}
            />
            <p>{otPack?.r}</p>

            <DownloadXMLFile content={serializeVoteToXML(
                new Vote(
                    voteSerial || "", selectedCode || "", authSerial || "", otPack?.authCode || "", serverSign || [])
            )} filename={"vote"} name={"DownloadXMLVoteFile"}/>

            <UploadSignedVote
                setVoterSign={setVoterSign}
                authCode={ otPack?.authCode || ""}
                voterSign={voterSign || ""}
                accessCode={ accessCode || ""}
            />
            <p>
                {commitment}
            </p>
            {successMessage !== null ? <Alert severity="success">{successMessage}</Alert> : <></>}
            {errorMessage !== null ? <Alert severity="error">{errorMessage}</Alert> : <></>}
        </>
    )
}