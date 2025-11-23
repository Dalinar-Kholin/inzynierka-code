import {type ChangeEvent, useContext} from "react";
import {BallotContext} from "../context/ballot/context.tsx";
import {Button} from "@mui/material";
import commitVote from "../api/commitVote.ts";
import {useAnchor} from "../hooks/useAnchor.ts";

export default function UploadSignedDocument() {
    const ballotCtx = useContext(BallotContext);
    const {getProgram, getProvider} = useAnchor()


    const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = () => {
            const text = String(reader.result ?? "");
            ballotCtx.setVoterSign(text);
        };
        reader.readAsText(file);
    };


    return <>
        <h2>upload signed Vote</h2>

        <input type="file" onChange={handleFileChange}/>

        <pre style={{marginTop: 16, maxHeight: 200, overflow: "auto"}}>
        {ballotCtx.ballot.VOTER_SIGN}
      </pre>
        <Button onClick={async () => commitVote({
            signedDocument: ballotCtx.ballot.VOTER_SIGN,
            authCode: ballotCtx.ballot.AUTH_CODE,
            program: getProgram(),
            provider: getProvider()
        })}>commit Vote</Button>
    </>
}