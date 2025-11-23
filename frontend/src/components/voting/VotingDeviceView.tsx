import {useContext, useState} from "react";
import {BallotContext} from "../../context/ballot/context.tsx";
import {Alert, Button} from "@mui/material";
import castVoteCode from "../../api/castVote.ts";
import {useAnchor} from "../../hooks/useAnchor.ts";
import getVoteCode from "../../api/getVoteCode.ts";
import getAuthCode from "../../api/getAuthCode.ts";
import pingServerForAcceptVote from "../../api/pingServerForAcceptVote.ts";
import BallotDataPirnt from "../BallotDataPirnt.tsx";
import DownloadXMLFile from "../downloadXMLFile.tsx";
import UploadSignedDocument from "../UploadSignedDocument.tsx";

export default function VotingDeviceView() {

    const [bit, setBit] = useState<boolean>(false);
    const ballotCtx = useContext(BallotContext);
    const { getProgram, getProvider } = useAnchor();
    const [successMessage, setSuccessMessage] = useState<string>("")
    const [errorMessage, setErrorMessage] = useState<string>("")

    return (<>

            <p>
                auth serial

                <input onChange={e => {
                    ballotCtx.setAuthSerial(e.target.value)
                }} value={ballotCtx.ballot.AUTH_SERIAL}/>
            </p>

        <p>
            vote serial
            <input onChange={e => {
                ballotCtx.setVoteSerial(e.target.value)
            }} value={ballotCtx.ballot.VOTE_SERIAL}/>
        </p>



        <p></p>
        {ballotCtx.ballot.VOTE_CODES.map(code =>
            <p key={code}>
                {<Button onClick={async () => {
                    ballotCtx.setSelectedCode(code)
                    castVoteCode({
                        voteCode: code,
                        authCode: ballotCtx.ballot.AUTH_CODE,
                        program: getProgram(),
                        provider: getProvider()
                    }).then(r => setSuccessMessage(`Vote transaction signature := ${r}`)).catch(e => setErrorMessage(e.message))
                }}>{code.toUpperCase()}</Button>}
            </p>
        )}

            <p>
            </p>
            <Button onClick={async () => {ballotCtx.setVoteCodes(await getVoteCode({voteSerial: ballotCtx.ballot.VOTE_SERIAL}))}}>get vote codes</Button>
            <p>
                Auth code Oblivious Transfer
                <Button onClick={() => setBit(b => !b)}>use {bit? "first" : "second"} AuthCod</Button>
                <Button onClick={async () => {
                    ballotCtx.setAuthCode((await getAuthCode({ authSerial: ballotCtx.ballot.AUTH_SERIAL, bit })).result)
                }}>get AuthCode</Button>
            </p>
            <p>
                ping server to Accept Vote
                <Button onClick={async () => await pingServerForAcceptVote({sign: "", authCode: ballotCtx.ballot.AUTH_CODE, voteSerial: ballotCtx.ballot.VOTE_SERIAL})}>AcceptVote</Button>
            </p>
            <BallotDataPirnt />
            <DownloadXMLFile />
            <UploadSignedDocument />
            {successMessage !== "" ? <Alert severity="success">{successMessage}</Alert> : <></>}
            {errorMessage !== "" ? <Alert severity="error">{errorMessage}</Alert> : <></>}
        </>
    )
}