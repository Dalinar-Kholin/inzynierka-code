import {Alert, Button} from "@mui/material";
import BallotDataPrint from "../BallotDataPrint.tsx";
import DownloadXMLFile from "../downloadXMLFile.tsx";
import UploadSignedVote from "../UploadSignedVote.tsx";
import ResponsiveDialog from "../obliviousTransferDialog.tsx";
import useVoting from "../../hooks/useVoting.ts";
import Vote, {serializeVoteToXML} from "../../XMLbuilder.ts";

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
            <form>
                <div>
                    <p>lockCode</p>
                    <p>
                        <input onChange={e => {
                            setLockCode(e.target.value)
                        }} value={lockCode}/>
                    </p>
                </div>
                <div>
                    <p>auth serial</p>
                    <p>
                        <input onChange={e => {
                            setAuthSerial(e.target.value)
                        }} value={authSerial}/>
                    </p>
                </div>
                <div>
                    <p>vote serial</p>
                    <p>
                        <input onChange={e => {setVoteSerial(e.target.value)}}
                               value={voteSerial}/>
                    </p>
                </div>
            </form>

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
                voteSerial={voteSerial} authSerial={authSerial} authCode={otPack?.authCode || ""}/>

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
            {successMessage !== null ? <Alert severity="success">{successMessage}</Alert> : <></>}
            {errorMessage !== null ? <Alert severity="error">{errorMessage}</Alert> : <></>}
        </>
    )
}